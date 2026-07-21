#version 450 core

in vec3 Normal;
in vec3 FragPos;
in float FragDistance;
in vec2 TexCoord;
in mat3 TBN;

out vec4 FragColor;

uniform sampler2D textureAtlas;
uniform sampler2D normalMap;
uniform bool useNormalMap;
uniform float normalStrength;
uniform float time;

uniform sampler2DArrayShadow shadowMap;

uniform samplerCube irradianceMap;
uniform samplerCube prefilterMap;
uniform sampler2D brdfLUT;
uniform bool useIBL;

// Screen-space reflections (research §7): reflect the actual opaque scene, IBL where rays miss.
uniform bool useSSR;
uniform sampler2D sceneColorTex;  // opaque HDR snapshot
uniform sampler2D sceneDepthTex;  // opaque reversed-Z depth
uniform mat4 ssrInvViewProj;      // reconstruct world pos from scene depth
uniform vec2 invScreenSize;       // for sampling the scene depth at this fragment

layout (std140, binding = 0) uniform SceneData {
    mat4 ViewProjection;
    vec4 ViewPos;
    vec4 FogColor;
    float FogNear;
    float FogFar;
    float Exposure;
    float Gamma;
    mat4 View;
};

struct DirLight {
    vec4 direction;
    vec4 color;
};

layout (std140, binding = 1) uniform LightingData {
    mat4 LightSpaceMatrix;
    DirLight dirLight;
};

#include "../Common/math.glsl"
#include "../Common/BRDF.glsl"
#include "../Common/shadows.glsl"
#include "../Common/clusters.glsl"

layout(std430, binding = 1) readonly buffer Lights          { Light lights[]; };
layout(std430, binding = 2) readonly buffer LightGrid       { uvec2 lightGrid[]; };
layout(std430, binding = 3) readonly buffer GlobalIndexList { uint globalLightIndex[]; };

uniform vec3 clusterGridSize;
uniform vec2 clusterScreenSize;
uniform float clusterZNear;
uniform float clusterZFar;

// Water properties - realistic clear lake water
const float WATER_IOR = 1.333;
const float WATER_ROUGHNESS = 0.02; // Very smooth water surface

// Realistic water colors (clear lake)
const vec3 WATER_ABSORPTION = vec3(0.45, 0.09, 0.06); // How much light is absorbed per meter
const vec3 WATER_SCATTER = vec3(0.02, 0.03, 0.04);    // Subsurface scatter color

// Advanced water properties
const float WATER_EXTINCTION_COEFF = 0.05;
const float WATER_SUBSURFACE_DEPTH = 2.0;

// Fresnel for water (Schlick approximation)
float fresnelWater(float cosTheta) {
    float f0 = pow((1.0 - WATER_IOR) / (1.0 + WATER_IOR), 2.0);
    return f0 + (1.0 - f0) * pow(1.0 - cosTheta, 5.0);
}

// Animated water normal from a sum-of-sines height field. The tangent-space normal is
// (-dH/dx, -dH/dz, 1): independent x/z derivatives with each wave traveling a different direction,
// so the normal is zero-mean with NO preferred tilt. (The previous version tied the x and z
// perturbations together — both = cos(wave) — which biased every normal along the x=z diagonal and
// skewed every reflection that same way.)
vec3 getWaterNormal(vec2 worldPos, float t) {
    vec2 dirs[3] = vec2[](vec2(0.9, 0.6), vec2(-0.5, 1.1), vec2(1.3, -0.35));
    float amps[3] = float[](0.06, 0.045, 0.03);
    float speeds[3] = float[](1.6, 1.2, 2.1);

    float dHdx = 0.0;
    float dHdz = 0.0;
    for (int i = 0; i < 3; i++) {
        float phase = dot(worldPos, dirs[i]) * 2.5 + t * speeds[i];
        float c = cos(phase) * amps[i];
        dHdx += c * dirs[i].x;
        dHdz += c * dirs[i].y;
    }

    vec3 n = normalize(vec3(-dHdx, -dHdz, 1.0));
    n.xy *= normalStrength * 0.3; // subtle waves
    return normalize(n);
}

// World-space SSR march with geometrically growing steps (precise near the surface, long reach for
// distant banks) and a binary refine on hit. Returns reflected color in .rgb, screen-edge
// confidence in .a (0 = miss). Camera distance is compared rather than raw depth to stay
// reversed-Z agnostic.
float ssrSceneDist(vec2 uv, vec3 viewPos) {
    float sd = texture(sceneDepthTex, uv).r;
    vec4 wH = ssrInvViewProj * vec4(uv * 2.0 - 1.0, sd, 1.0);
    return distance(wH.xyz / wH.w, viewPos);
}

vec4 traceSSR(vec3 worldPos, vec3 reflectDir, vec3 viewPos) {
    const int STEPS = 48;
    vec3 rayPos = worldPos + reflectDir * 0.2;
    float stepLen = 0.4;
    vec3 prevPos = rayPos;

    for (int i = 0; i < STEPS; i++) {
        prevPos = rayPos;
        rayPos += reflectDir * stepLen;
        stepLen = min(stepLen * 1.2, 8.0); // grow, then plateau at 8u so far steps stay precise

        vec4 clip = ViewProjection * vec4(rayPos, 1.0);
        if (clip.w <= 0.0) break;
        vec2 uv = (clip.xy / clip.w) * 0.5 + 0.5;
        if (any(lessThan(uv, vec2(0.0))) || any(greaterThan(uv, vec2(1.0)))) break;

        if (texture(sceneDepthTex, uv).r <= 0.0) continue; // sky — nothing to reflect

        float rayDist = distance(rayPos, viewPos);
        float sceneDist = ssrSceneDist(uv, viewPos);
        if (rayDist > sceneDist && rayDist - sceneDist < stepLen + 1.0) {
            // Binary refine between the last in-front position and this behind-surface one.
            vec3 a = prevPos, b = rayPos;
            for (int j = 0; j < 5; j++) {
                vec3 mid = (a + b) * 0.5;
                vec4 mc = ViewProjection * vec4(mid, 1.0);
                vec2 muv = (mc.xy / mc.w) * 0.5 + 0.5;
                if (distance(mid, viewPos) > ssrSceneDist(muv, viewPos)) b = mid; else a = mid;
            }
            vec4 fc = ViewProjection * vec4(b, 1.0);
            vec2 fuv = (fc.xy / fc.w) * 0.5 + 0.5;
            vec2 edge = smoothstep(0.0, 0.12, fuv) * smoothstep(1.0, 0.88, fuv);
            return vec4(texture(sceneColorTex, fuv).rgb, edge.x * edge.y);
        }
    }
    return vec4(0.0);
}

void main() {
    vec3 V = normalize(ViewPos.xyz - FragPos);
    
    // Get animated water normal
    vec3 localNormal = getWaterNormal(FragPos.xz * 0.1, time);
    vec3 N = normalize(TBN * localNormal);
    
    // View-dependent factors
    float NoV = max(dot(N, V), 0.0);
    float fresnel = fresnelWater(NoV);
    
    // === LIGHTING ===
    vec3 L = normalize(-dirLight.direction.xyz);
    float sunHeight = max(L.y, 0.0);
    
    // Shadow calculation. LightSpaceMatrix carries cascade 0 (nearest) of the CSM array;
    // distant water simply falls outside it and reads as lit (acceptable for transparent water).
    vec4 fragPosLightSpace = LightSpaceMatrix * vec4(FragPos, 1.0);
    float shadow = 0.0;
    if (length(dirLight.color.xyz) > 0.0) {
        shadow = CalcShadowCSM(shadowMap, fragPosLightSpace, 0, N, L, 0.01);
    }
    
    // Effective light (reduced by shadow)
    float lightFactor = (1.0 - shadow) * sunHeight;
    vec3 lightColor = dirLight.color.xyz * lightFactor;
    
    // Moonlight
    vec3 moonDir = normalize(dirLight.direction.xyz);
    float sunVisible = clamp(L.y * 5.0, 0.0, 1.0);
    float moonIntensity = (1.0 - sunVisible) * 0.08;
    vec3 moonLightColor = vec3(0.4, 0.5, 0.7) * moonIntensity;
    
    // === ADVANCED WATER COLOR WITH SUBSURFACE SCATTERING ===
    // Simulate light penetrating water and scattering
    vec3 waterTint = vec3(0.15, 0.25, 0.3); // Subtle blue-green tint

    // Depth-based absorption (Beer-Lambert law approximation)
    float depthFactor = pow(1.0 - NoV, 3.0);
    vec3 deepColor = vec3(0.02, 0.08, 0.12); // Dark blue-green for "deep" water

    // Subsurface scattering approximation
    float VoL = max(dot(V, -L), 0.0);
    float subsurfaceContrib = pow(VoL, 4.0) * (1.0 - fresnel);

    // Scatter color modulated by depth
    vec3 subsurface = WATER_SCATTER * subsurfaceContrib * sunHeight;

    vec3 baseWaterColor = mix(waterTint, deepColor, depthFactor * 0.5) + subsurface;

    // === SPECULAR REFLECTION ===
    vec3 H = normalize(V + L);
    float NoH = max(dot(N, H), 0.0);
    float NoL = max(dot(N, L), 0.0);
    float HoV = max(dot(H, V), 0.0);
    
    // GGX specular for sun
    float D = DistributionGGX(N, H, WATER_ROUGHNESS);
    float G = GeometrySmith(N, V, L, WATER_ROUGHNESS);
    vec3 F = vec3(fresnelWater(HoV));
    
    vec3 specular = (D * G * F) / max(4.0 * NoV * NoL, 0.001);
    vec3 sunSpec = specular * lightColor * NoL;
    
    // Moon specular (very subtle)
    if (moonIntensity > 0.0) {
        vec3 Hm = normalize(V + moonDir);
        float NoHm = max(dot(N, Hm), 0.0);
        float Dm = DistributionGGX(N, Hm, WATER_ROUGHNESS);
        sunSpec += vec3(Dm * 0.1) * moonLightColor;
    }
    
    // Point light specular
    {
        float viewZ = (View * vec4(FragPos, 1.0)).z;
        uint cluster = clusterIndex(gl_FragCoord.xy, viewZ, clusterScreenSize,
                                    uvec3(clusterGridSize), clusterZNear, clusterZFar);
        uint offset = lightGrid[cluster].x;
        uint count = lightGrid[cluster].y;
        for (uint li = 0u; li < count; li++) {
            Light light = lights[globalLightIndex[offset + li]];
            vec3 d = light.positionRange.xyz - FragPos;
            float dist = length(d);
            if (dist > light.positionRange.w) continue;
            float att = 1.0 / (light.atten.x + light.atten.y * dist + light.atten.z * dist * dist);
            vec3 radiance = light.color.rgb * light.color.w * att;
            vec3 Lp = normalize(d);
            vec3 Hp = normalize(V + Lp);
            float NoHp = max(dot(N, Hp), 0.0);
            float Dp = DistributionGGX(N, Hp, WATER_ROUGHNESS);
            sunSpec += vec3(Dp * 0.5) * radiance;
        }
    }
    
    // === SKY REFLECTION WITH REFRACTION ===
    vec3 R = reflect(-V, N);

    // Refraction (simulate light bending through water)
    vec3 T = refract(-V, N, 1.0 / WATER_IOR);
    bool hasTotalInternalReflection = (length(T) < 0.01);

    vec3 skyReflection;

    if (useIBL) {
        // Glossy reflection from the prefiltered env map — the distant/fallback reflection. SSR
        // below replaces it with the actual scene where rays hit on-screen geometry.
        float reflectionLOD = 1.0;
        skyReflection = textureLod(prefilterMap, R, reflectionLOD).rgb;

        // Wave-driven distortion to break up the reflection.
        vec3 distortedR = normalize(R + N * 0.05);
        vec3 distortedRefl = textureLod(prefilterMap, distortedR, reflectionLOD).rgb;
        skyReflection = mix(skyReflection, distortedRefl, 0.25);
    } else {
        // Approximate sky color based on reflection direction
        float skyGrad = max(R.y, 0.0);
        vec3 skyZenith = vec3(0.1, 0.2, 0.4) * (sunHeight + 0.1);
        vec3 skyHorizon = vec3(0.3, 0.35, 0.45) * (sunHeight + 0.1);
        skyReflection = mix(skyHorizon, skyZenith, pow(skyGrad, 0.8));

        // Add sun reflection in sky with more realistic falloff
        float sunRefl = pow(max(dot(R, L), 0.0), 128.0);
        skyReflection += dirLight.color.xyz * sunRefl * sunHeight * 2.0;

        // Add horizon glow
        float horizonGlow = pow(1.0 - abs(R.y), 4.0);
        skyReflection += vec3(0.8, 0.5, 0.3) * horizonGlow * sunHeight * 0.2;
    }

    // Screen-space reflections: replace the IBL/sky fallback with the real scene where the
    // reflected ray hits on-screen geometry (research §7). Confidence fades at screen edges.
    if (useSSR) {
        vec4 ssr = traceSSR(FragPos, R, ViewPos.xyz);
        skyReflection = mix(skyReflection, ssr.rgb, ssr.a);
    }

    // === REAL WATER DEPTH (surface → bed) via the opaque scene depth ===
    // Shallow water shows the bottom; deep water absorbs the light and hides it (Beer-Lambert).
    float waterDepth = 0.0;
    if (useSSR) {
        vec2 sUV = gl_FragCoord.xy * invScreenSize;
        float bedDepth = texture(sceneDepthTex, sUV).r;
        if (bedDepth > 0.0) {
            vec4 bedH = ssrInvViewProj * vec4(sUV * 2.0 - 1.0, bedDepth, 1.0);
            waterDepth = max(distance(FragPos, bedH.xyz / bedH.w), 0.0);
        }
    }
    float depthOpacity = 1.0 - exp(-waterDepth * 0.35); // 0 at the shore, →1 over deep water

    // === COMBINE ===
    // Water is mostly transparent with some tint, plus reflections. Deep water darkens toward the
    // deep color as the bed is absorbed out.
    vec3 diffuse = baseWaterColor * (lightColor + moonLightColor + vec3(0.02)) * 0.3;
    diffuse = mix(diffuse, deepColor * (lightColor + 0.05), depthOpacity * 0.6);
    vec3 reflection = skyReflection * fresnel;

    vec3 waterColor = diffuse + reflection + sunSpec;

    // === TRANSPARENCY WITH DEPTH FADE ===
    // Water is more transparent when looking straight down, more reflective at glancing angles.
    // A higher floor keeps it reading as water (not glass) even looking straight down.
    float baseAlpha = mix(0.45, 0.9, fresnel);

    // Real depth fade: deep water becomes opaque so the lit bed doesn't show through unnaturally.
    baseAlpha = mix(baseAlpha, 0.96, depthOpacity);

    // Depth-based transparency fade (clearer at shallow angles)
    float depthTransparency = 1.0 - exp(-depthFactor * 0.5);
    baseAlpha = mix(baseAlpha, baseAlpha * 1.2, depthTransparency);

    // Increase opacity in shadow and at night
    baseAlpha = mix(baseAlpha, baseAlpha * 1.3, shadow * 0.3);
    baseAlpha = mix(baseAlpha, 0.75, (1.0 - sunHeight) * 0.3);

    // Foam at edges (where water meets geometry)
    float foamFactor = pow(1.0 - NoV, 8.0);
    baseAlpha = mix(baseAlpha, 0.95, foamFactor * 0.3);

    float alpha = clamp(baseAlpha, 0.4, 0.97);
    
    // Output linear HDR — tone mapping is handled by the post-process pass (fxaa.frag)
    FragColor = vec4(waterColor, alpha);
}
