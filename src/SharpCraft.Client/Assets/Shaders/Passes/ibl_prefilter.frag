#version 450 core
out vec4 FragColor;

in vec3 LocalPos;

uniform samplerCube environmentMap;
uniform float roughness;
uniform float resolution; // source cube face resolution (for mip selection)

#include "../Common/ibl_common.glsl"

// Prefiltered specular environment via the split-sum approximation (research §4.2/§6).
// Mip level = roughness. Samples a mip of the source by solid-angle to suppress fireflies.
void main() {
    vec3 N = normalize(LocalPos);
    vec3 R = N;
    vec3 V = R;

    const uint SAMPLE_COUNT = 256u;
    vec3 prefilteredColor = vec3(0.0);
    float totalWeight = 0.0;

    for (uint i = 0u; i < SAMPLE_COUNT; i++) {
        vec2 Xi = Hammersley(i, SAMPLE_COUNT);
        vec3 H = ImportanceSampleGGX(Xi, N, roughness);
        vec3 L = normalize(2.0 * dot(V, H) * H - V);

        float NdotL = max(dot(N, L), 0.0);
        if (NdotL > 0.0) {
            // Solid-angle mip selection (Karis) — sample blurrier mips for high-pdf samples.
            float NdotH = max(dot(N, H), 0.0);
            float HdotV = max(dot(H, V), 0.0);
            float a = roughness * roughness;
            float D = (a * a) / max(PI * pow(NdotH * NdotH * (a * a - 1.0) + 1.0, 2.0), 1e-7);
            float pdf = (D * NdotH / (4.0 * HdotV)) + 0.0001;

            float saTexel = 4.0 * PI / (6.0 * resolution * resolution);
            float saSample = 1.0 / (float(SAMPLE_COUNT) * pdf + 0.0001);
            float mipLevel = roughness == 0.0 ? 0.0 : 0.5 * log2(saSample / saTexel);

            prefilteredColor += textureLod(environmentMap, L, mipLevel).rgb * NdotL;
            totalWeight += NdotL;
        }
    }

    prefilteredColor = prefilteredColor / max(totalWeight, 0.0001);
    FragColor = vec4(prefilteredColor, 1.0);
}
