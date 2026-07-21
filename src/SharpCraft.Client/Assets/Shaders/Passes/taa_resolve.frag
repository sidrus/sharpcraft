#version 450 core

// Temporal anti-aliasing resolve (research §9). The scene is rendered each frame with a sub-pixel
// jittered projection; this pass reprojects the accumulated history into the current frame and
// blends, so the jitter integrates into smooth edges over time. The voxel world is fully static,
// so reprojection is depth-based (reconstruct world from depth, project through the previous
// frame's view-projection) — no per-object motion vectors needed.

out vec4 FragColor;
in vec2 TexCoords;

uniform sampler2D currentColor;  // this frame's jittered HDR scene
uniform sampler2D historyColor;  // previous resolved HDR
uniform sampler2D depthTexture;  // this frame's reversed-Z depth

uniform mat4 invViewProj;        // inverse of the current (jittered) view-projection
uniform mat4 prevViewProj;       // previous frame's (jittered) view-projection
uniform vec2 texelSize;
uniform float blendFactor;       // current-frame weight (history weight = 1 - blendFactor)
uniform bool historyValid;

// YCoCg is the standard space for TAA neighborhood clipping (luma/chroma separation reduces
// clipping artifacts vs RGB).
vec3 rgbToYCoCg(vec3 c) {
    return vec3(
         0.25 * c.r + 0.5 * c.g + 0.25 * c.b,
         0.5  * c.r            - 0.5  * c.b,
        -0.25 * c.r + 0.5 * c.g - 0.25 * c.b);
}

vec3 yCoCgToRgb(vec3 c) {
    float t = c.x - c.z;
    return vec3(t + c.y, c.x + c.z, t - c.y);
}

void main() {
    vec3 current = texture(currentColor, TexCoords).rgb;

    if (!historyValid) {
        FragColor = vec4(current, 1.0);
        return;
    }

    // Reconstruct world position from reversed-Z depth (ZeroToOne: NDC z = stored depth directly).
    // Clamp far depth away from 0 so sky/far pixels reconstruct at a large but finite distance and
    // reproject by camera rotation instead of producing a degenerate point at infinity.
    float depth = max(texture(depthTexture, TexCoords).r, 1e-4);
    vec4 ndc = vec4(TexCoords * 2.0 - 1.0, depth, 1.0);
    vec4 worldH = invViewProj * ndc;
    vec3 worldPos = worldH.xyz / worldH.w;

    // Reproject into the previous frame.
    vec4 prevClip = prevViewProj * vec4(worldPos, 1.0);
    vec2 prevUV = (prevClip.xy / prevClip.w) * 0.5 + 0.5;

    if (any(lessThan(prevUV, vec2(0.0))) || any(greaterThan(prevUV, vec2(1.0)))) {
        FragColor = vec4(current, 1.0); // reprojected off-screen — no valid history
        return;
    }

    vec3 history = texture(historyColor, prevUV).rgb;

    // Neighborhood variance clipping (YCoCg): clamp history to the local color AABB to reject
    // ghosting where the scene changed.
    vec3 cMin = vec3(1e9);
    vec3 cMax = vec3(-1e9);
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            vec3 n = rgbToYCoCg(texture(currentColor, TexCoords + vec2(x, y) * texelSize).rgb);
            cMin = min(cMin, n);
            cMax = max(cMax, n);
        }
    }
    vec3 histYCoCg = clamp(rgbToYCoCg(history), cMin, cMax);
    history = yCoCgToRgb(histYCoCg);

    vec3 result = mix(history, current, blendFactor);
    FragColor = vec4(result, 1.0);
}
