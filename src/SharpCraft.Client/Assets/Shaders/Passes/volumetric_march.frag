#version 450 core

// Volumetric fog + sun light shafts (research §11 step 10). Ray-march from the camera through a
// height-fog medium, sampling the cascaded shadow map at each step so in-scattered sunlight is
// occluded by geometry (crepuscular shafts / god rays). Output: rgb = in-scatter, a = Beer-Lambert
// transmittance over the marched segment. Half-resolution; composited into the HDR scene afterward.

out vec4 FragColor;
in vec2 TexCoords;

#include "../Common/math.glsl"

uniform sampler2D depthTexture;            // main pass reversed-Z depth (NDC z in [0,1])
uniform sampler2DArrayShadow shadowMap;    // CSM cascades

// Cascaded shadow maps (research §8) — must match terrain.frag's block layout exactly.
layout (std140, binding = 2) uniform CsmData {
    mat4 lightSpaceMatrices[4];
    vec4 cascadeSplits; // x..w = far view-distance of each cascade
    vec4 csmParams;     // x = cascadeCount, y = shadowMapSize
};

uniform mat4 invViewProj;   // inverse of the jittered main view-projection
uniform vec3 cameraPos;
uniform vec3 sunDirection;   // normalized, points TOWARD the sun
uniform vec3 sunColor;       // sun color × intensity (≈0 at night → no shafts)
uniform vec3 fogColor;       // ambient/sky tint for shadowed fog
uniform float density;       // base scattering/extinction coefficient at the fog floor
uniform float extinction;    // extra extinction scale (haze that attenuates without scattering)
uniform float intensity;     // in-scatter strength multiplier
uniform int steps;           // march sample count
uniform float mieG;          // Henyey-Greenstein anisotropy (forward scattering)
uniform float maxDistance;   // cap the march length (sky rays would be unbounded otherwise)
uniform float frameJitter;   // per-frame [0,1) offset, animates the dither for TAA to clean up

// Height-fog profile. A slab centered near sea level: density thins exponentially with ALTITUDE
// above the base, and — critically — fades to zero a few blocks BELOW it. The fade-below matters
// because this is air fog: without it the column under a lake is treated as full-density fog, so
// the march from an above-water camera down to the submerged bed accumulates huge in-scatter and
// washes the water surface out entirely (the "missing water" over deep basins). Underwater light
// absorption is water.frag's job, not the air fog's. Tunable; HUD density/intensity compensate.
const float FOG_BASE_HEIGHT = 63.0;    // ~water surface / sea level
const float FOG_HEIGHT_FALLOFF = 0.04; // e-fold over ~25 blocks (above the base)
const float FOG_FLOOR_FADE = 8.0;      // fog fades to zero this many blocks below the base (under water)
const float AMBIENT_SCATTER = 0.12;    // fraction of fogColor scattered in shadow (keeps fog from going black)

// Henyey-Greenstein phase function.
float phaseHG(float cosTheta, float g) {
    float g2 = g * g;
    float denom = 1.0 + g2 - 2.0 * g * cosTheta;
    return (1.0 / (4.0 * PI)) * (1.0 - g2) / (denom * sqrt(max(denom, 1e-4)));
}

// Sun visibility at a world position via the cascaded shadow map (single hardware-PCF tap).
float sunVisibility(vec3 worldPos) {
    int cascadeCount = int(csmParams.x);
    float viewDist = length(worldPos - cameraPos);
    int layer = cascadeCount - 1;
    for (int i = 0; i < cascadeCount; i++) {
        if (viewDist < cascadeSplits[i]) { layer = i; break; }
    }

    vec4 lp = lightSpaceMatrices[layer] * vec4(worldPos, 1.0);
    vec3 pc = lp.xyz / lp.w;
    pc.xy = pc.xy * 0.5 + 0.5;
    if (pc.z > 1.0 || any(lessThan(pc.xy, vec2(0.0))) || any(greaterThan(pc.xy, vec2(1.0))))
        return 1.0; // outside this cascade → treat as lit
    return texture(shadowMap, vec4(pc.xy, float(layer), pc.z - 0.0008));
}

void main() {
    vec2 ndc = TexCoords * 2.0 - 1.0;

    // Per-pixel view ray (robust mid-depth reconstruction, as in skybox.frag).
    vec4 midH = invViewProj * vec4(ndc, 0.5, 1.0);
    vec3 rayDir = normalize(midH.xyz / midH.w - cameraPos);

    // March length: to the opaque scene if present, else capped (sky pixels read depth 0 = far).
    float depth = texture(depthTexture, TexCoords).r;
    float rayLen;
    if (depth <= 0.0) {
        rayLen = maxDistance;
    } else {
        vec4 wH = invViewProj * vec4(ndc, depth, 1.0);
        vec3 worldPos = wH.xyz / wH.w;
        rayLen = min(length(worldPos - cameraPos), maxDistance);
    }

    int n = max(steps, 1);
    float dt = rayLen / float(n);

    // Dither the march start to break up banding; animated per frame for TAA to resolve.
    float dither = fract(dot(gl_FragCoord.xy, vec2(0.7548776662, 0.5698402909)) + frameJitter);

    float phase = phaseHG(dot(rayDir, sunDirection), mieG);
    vec3 ambient = fogColor * AMBIENT_SCATTER;

    vec3 inscatter = vec3(0.0);
    float transmittance = 1.0;

    for (int i = 0; i < n; i++) {
        float t = (float(i) + dither) * dt;
        vec3 p = cameraPos + rayDir * t;

        float heightFog = exp(-max(p.y - FOG_BASE_HEIGHT, 0.0) * FOG_HEIGHT_FALLOFF)  // thin with altitude
                        * smoothstep(FOG_BASE_HEIGHT - FOG_FLOOR_FADE, FOG_BASE_HEIGHT, p.y); // fade out below (under water)
        float dens = density * heightFog;
        if (dens <= 0.0) continue;

        float sigmaT = dens * (1.0 + extinction / max(density, 1e-5));
        float vis = sunVisibility(p);

        // In-scattered light: direct sun (phase-weighted, shadowed) + isotropic ambient fill.
        vec3 stepScatter = (sunColor * phase * vis + ambient) * dens * dt;
        inscatter += transmittance * stepScatter;
        transmittance *= exp(-sigmaT * dt);
    }

    // 'intensity' is a MASTER FADE for the whole effect, not just an in-scatter gain. It scales the
    // in-scatter AND softens the extinction toward 1, i.e. composites mix(scene, foggedScene, fade).
    // Scaling in-scatter alone (the naive way) keeps full extinction at low intensity, darkening the
    // scene and crushing shadow detail — lowering this now fades fog out cleanly instead. The blend
    // (ONE, SRC_ALPHA) yields scene·T' + I' with T' = 1 - fade·(1-T), I' = fade·inscatter.
    float fade = clamp(intensity, 0.0, 1.0);
    float Tout = 1.0 - fade * (1.0 - transmittance);
    FragColor = vec4(inscatter * intensity, Tout); // inscatter keeps full intensity for god-ray punch >1
}
