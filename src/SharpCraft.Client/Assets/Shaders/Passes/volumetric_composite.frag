#version 450 core

// Composite the half-res volumetric scatter target into the HDR scene. The blend itself is
// fixed-function: glBlendFunc(ONE, SRC_ALPHA) gives scene·transmittance + inscatter, where this
// shader's output rgb = inscatter and alpha = transmittance.
//
// Depth-aware (bilateral) upsample: a plain bilinear upsample bleeds the sky's heavy in-scatter
// across terrain silhouettes (the half-res sky texel = long march, the neighboring terrain texel =
// short march), drawing a bright fringe on terrain edges. We instead weight the 2x2 half-res
// neighborhood by depth similarity to the full-res pixel, so samples belonging to the wrong surface
// (the sky, across an edge) are rejected and the halo disappears.

out vec4 FragColor;
in vec2 TexCoords;

uniform sampler2D scatterTexture; // half-res: rgb = inscatter, a = transmittance
uniform sampler2D depthTexture;   // full-res reversed-Z depth (the march sampled this)
uniform float near;               // camera near plane, for linearizing reversed-Z depth

// Infinite reversed-Z: ndc.z = near / viewZ → viewZ = near / depth. Sky (depth ≤ 0) is "infinitely"
// far, which makes it strongly dissimilar to any terrain sample and thus rejected at edges.
float linearizeRevZ(float d) {
    return (d <= 0.0) ? 1e8 : near / d;
}

void main() {
    float dRef = linearizeRevZ(texture(depthTexture, TexCoords).r);

    vec2 hres = vec2(textureSize(scatterTexture, 0));
    vec2 halfTexel = 1.0 / hres;

    // 2x2 half-res neighborhood (bilinear footprint) around this full-res pixel.
    vec2 coord = TexCoords * hres - 0.5;
    vec2 base = floor(coord);
    vec2 f = coord - base;
    vec2 uv00 = (base + 0.5) * halfTexel;

    vec2 offs[4] = vec2[](vec2(0, 0), vec2(1, 0), vec2(0, 1), vec2(1, 1));
    float bw[4] = float[]((1.0 - f.x) * (1.0 - f.y), f.x * (1.0 - f.y),
                          (1.0 - f.x) * f.y,         f.x * f.y);

    vec4 sum = vec4(0.0);
    float wsum = 0.0;
    for (int i = 0; i < 4; i++) {
        vec2 uv = uv00 + offs[i] * halfTexel;
        float dS = linearizeRevZ(texture(depthTexture, uv).r);
        // Depth similarity, relative to the reference distance (fog is smooth, so allow gentle
        // in-surface blending while rejecting cross-silhouette samples).
        float depthW = exp(-abs(dRef - dS) / (0.1 * dRef + 1.0));
        float w = bw[i] * depthW + 1e-5;
        sum += texture(scatterTexture, uv) * w;
        wsum += w;
    }

    FragColor = sum / wsum;
}
