#version 450 core

// Ground-truth-style ambient occlusion (research §7). Runs off the depth pre-pass only: view
// position and a geometric normal are reconstructed from depth (clean on flat voxel faces), then a
// horizon/hemisphere search estimates how occluded the ambient hemisphere is. The result multiplies
// into the IBL/ambient term in the forward pass. Per-pixel rotation keeps the sample pattern noisy;
// TAA averages the noise away over frames, so no separate blur pass is needed.

out float FragColor;
in vec2 TexCoords;

uniform sampler2D depthTexture;   // reversed-Z depth from the pre-pass
uniform mat4 invProjection;       // inverse of the (jittered) main projection — clip → view
uniform vec2 projScale;           // (proj.M11, proj.M22) for projecting the view-space radius
uniform vec2 texelSize;
uniform float radius;             // view-space sampling radius (world units)
uniform float intensity;
uniform float frameJitter;        // per-frame rotation offset so TAA sees fresh samples

#define DIRECTIONS 8
#define STEPS 4
#define PI 3.14159265

float hash(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

// Clip+depth → view-space position (reversed-Z ZeroToOne: NDC z = stored depth).
vec3 viewPosFromDepth(vec2 uv, float depth) {
    vec4 clip = vec4(uv * 2.0 - 1.0, depth, 1.0);
    vec4 v = invProjection * clip;
    return v.xyz / v.w;
}

void main() {
    float depth = texture(depthTexture, TexCoords).r;
    if (depth <= 0.0) { FragColor = 1.0; return; } // sky / far plane — no occlusion

    vec3 P = viewPosFromDepth(TexCoords, depth);

    // Geometric view normal from depth, using the nearer neighbor on each axis to avoid bleeding
    // across silhouette edges.
    vec3 Pr = viewPosFromDepth(TexCoords + vec2(texelSize.x, 0.0), texture(depthTexture, TexCoords + vec2(texelSize.x, 0.0)).r);
    vec3 Pl = viewPosFromDepth(TexCoords - vec2(texelSize.x, 0.0), texture(depthTexture, TexCoords - vec2(texelSize.x, 0.0)).r);
    vec3 Pu = viewPosFromDepth(TexCoords + vec2(0.0, texelSize.y), texture(depthTexture, TexCoords + vec2(0.0, texelSize.y)).r);
    vec3 Pd = viewPosFromDepth(TexCoords - vec2(0.0, texelSize.y), texture(depthTexture, TexCoords - vec2(0.0, texelSize.y)).r);
    vec3 ddx = (abs(Pr.z - P.z) < abs(P.z - Pl.z)) ? (Pr - P) : (P - Pl);
    vec3 ddy = (abs(Pu.z - P.z) < abs(P.z - Pd.z)) ? (Pu - P) : (P - Pd);
    vec3 N = normalize(cross(ddx, ddy));
    if (dot(N, P) > 0.0) N = -N; // face the camera

    // Project the world-space radius into screen UV at this depth, capped so very near surfaces
    // don't sample halfway across the screen.
    vec2 radiusUV = min(radius * 0.5 * projScale / max(-P.z, 1e-3), vec2(0.08));

    float rot = (hash(gl_FragCoord.xy) + frameJitter) * 2.0 * PI;
    float occlusion = 0.0;

    for (int d = 0; d < DIRECTIONS; d++) {
        float a = rot + float(d) / float(DIRECTIONS) * 2.0 * PI;
        vec2 dir = vec2(cos(a), sin(a));
        for (int s = 0; s < STEPS; s++) {
            float t = (float(s) + 1.0) / float(STEPS);
            vec2 sUV = TexCoords + dir * radiusUV * t;
            if (sUV.x < 0.0 || sUV.x > 1.0 || sUV.y < 0.0 || sUV.y > 1.0) continue;

            float sDepth = texture(depthTexture, sUV).r;
            if (sDepth <= 0.0) continue;
            vec3 sP = viewPosFromDepth(sUV, sDepth);

            vec3 diff = sP - P;
            float dist = length(diff);
            if (dist < 1e-4) continue;
            float falloff = clamp(1.0 - dist / radius, 0.0, 1.0);
            float ndl = max(dot(N, diff / dist), 0.0);
            occlusion += ndl * falloff;
        }
    }

    occlusion = occlusion / float(DIRECTIONS * STEPS) * intensity;
    FragColor = clamp(1.0 - occlusion, 0.0, 1.0);
}
