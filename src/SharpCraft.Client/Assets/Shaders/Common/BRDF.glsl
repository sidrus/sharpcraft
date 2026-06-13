#ifndef BRDF_GLSL
#define BRDF_GLSL

#include "math.glsl"

// Cook-Torrance microfacet BRDF (research §3.1): GGX D, height-correlated Smith V, Schlick F.

// D — GGX / Trowbridge-Reitz normal distribution. Takes perceptual roughness; α = roughness².
float DistributionGGX(vec3 N, vec3 H, float roughness) {
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;

    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;
    return a2 / max(denom, 1e-7);
}

// V — height-correlated Smith visibility (Heitz 2014), folds in the 1/(4·NoV·NoL) term.
// alpha is the GGX α (= perceptual roughness²).
float V_SmithGGXCorrelated(float NoV, float NoL, float alpha) {
    float a2 = alpha * alpha;
    float ggxV = NoL * sqrt(NoV * NoV * (1.0 - a2) + a2);
    float ggxL = NoV * sqrt(NoL * NoL * (1.0 - a2) + a2);
    return 0.5 / max(ggxV + ggxL, 1e-5);
}

// Separable Schlick-GGX geometry term (direct-lighting k). Retained for the water shader's
// own Cook-Torrance evaluation; the main BRDF uses V_SmithGGXCorrelated above.
float GeometrySchlickGGX(float NdotV, float roughness) {
    float r = (roughness + 1.0);
    float k = (r * r) / 8.0;
    return NdotV / max(NdotV * (1.0 - k) + k, 1e-7);
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness) {
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    return GeometrySchlickGGX(NdotV, roughness) * GeometrySchlickGGX(NdotL, roughness);
}

// F — Schlick Fresnel.
vec3 fresnelSchlick(float cosTheta, vec3 F0) {
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

// Roughness-aware Fresnel for the IBL ambient term (research §4.2). Used by the IBL round.
vec3 fresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness) {
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

// Specular occlusion from AO/roughness/NoV (Lagarde, research §4.3). Used by the IBL round.
float computeSpecularAO(float NoV, float ao, float roughness) {
    return clamp(pow(NoV + ao, exp2(-16.0 * roughness - 1.0)) - 1.0 + ao, 0.0, 1.0);
}

// Geometric specular anti-aliasing (research §3.5): widen roughness to cover sub-pixel normal
// variance, killing specular shimmer on sharp voxel normals.
float specularAntiAliasing(vec3 normal, float roughness) {
    const float SIGMA2 = 0.25; // variance scale
    const float KAPPA = 0.18;  // clamp

    vec3 dndu = dFdx(normal);
    vec3 dndv = dFdy(normal);
    float variance = SIGMA2 * (dot(dndu, dndu) + dot(dndv, dndv));
    float kernelRoughness = min(2.0 * variance, KAPPA);
    return clamp(roughness + kernelRoughness, 0.0, 1.0);
}

#endif
