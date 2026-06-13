#ifndef LIGHTING_GLSL
#define LIGHTING_GLSL

#include "math.glsl"
#include "BRDF.glsl"

// ============================================================================
// Analytic (punctual) PBR lighting — Cook-Torrance specular + Lambert diffuse
// (research §3.1–§3.2, §4.1). Single-scatter; multi-scatter energy compensation
// arrives with the DFG LUT in the IBL round (§3.3/§6).
// ============================================================================

vec3 CalcPBRLighting(vec3 L, vec3 V, vec3 N, vec3 F0, vec3 albedo, float metallic, float roughness, vec3 lightColor) {
    vec3 H = normalize(V + L);
    float NoV = max(dot(N, V), 1e-4);
    float NoL = max(dot(N, L), 1e-4);
    float VoH = max(dot(V, H), 0.0);

    float alpha = roughness * roughness; // GGX α

    // Specular = D · V · F  (V folds in the 1/(4·NoV·NoL) denominator).
    float D   = DistributionGGX(N, H, roughness);
    float Vis = V_SmithGGXCorrelated(NoV, NoL, alpha);
    vec3  F   = fresnelSchlick(VoH, F0);
    vec3 specular = D * Vis * F;

    // Lambert diffuse. Metals have no diffuse; (1 - F) keeps diffuse+specular energy-conserving.
    vec3 kD = (vec3(1.0) - F) * (1.0 - metallic);
    vec3 diffuse = kD * albedo / PI;

    return (diffuse + specular) * lightColor * NoL;
}

#endif
