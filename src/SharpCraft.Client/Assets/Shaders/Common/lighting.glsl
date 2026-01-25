#ifndef LIGHTING_GLSL
#define LIGHTING_GLSL

#include "math.glsl"
#include "BRDF.glsl"

// ============================================================================
// PBR Lighting Calculation (Cook-Torrance BRDF)
// ============================================================================

vec3 CalcPBRLighting(vec3 L, vec3 V, vec3 N, vec3 F0, vec3 albedo, float metallic, float roughness, vec3 lightColor) {
    vec3 H = normalize(V + L);
    float NoV = max(dot(N, V), 0.001);
    float NoL = max(dot(N, L), 0.001);
    float NoH = max(dot(N, H), 0.0);
    float HoV = max(dot(H, V), 0.0);
    float LoH = max(dot(L, H), 0.0);
    
    // Cook-Torrance BRDF
    float NDF = DistributionGGX(N, H, roughness);
    float G   = GeometrySmith(N, V, L, roughness);
    vec3 F    = fresnelSchlick(HoV, F0);
    
    // Specular BRDF
    vec3 numerator    = NDF * G * F;
    float denominator = 4.0 * NoV * NoL + 0.0001;
    vec3 specular = numerator / denominator;
    
    // Multi-scatter energy compensation (Kulla-Conty)
    vec3 energyCompensation = getMultiScatteringCompensation(NoV, NoL, roughness, F0);
    specular += energyCompensation * (1.0 - F) * NoL;
    
    // Diffuse/specular split
    vec3 kS = F;
    vec3 kD = vec3(1.0) - kS;
    kD *= 1.0 - metallic;
    
    // Lambertian diffuse
    vec3 diffuse = kD * albedo / PI;
    
    return (diffuse + specular) * lightColor * NoL;
}

// ============================================================================
// Area Light Approximations (for future use)
// ============================================================================

// Representative point method for sphere lights
vec3 getRepresentativePoint(vec3 L, vec3 V, vec3 N, float lightRadius) {
    vec3 R = reflect(-V, N);
    vec3 centerToRay = dot(L, R) * R - L;
    vec3 closestPoint = L + centerToRay * clamp(lightRadius / length(centerToRay), 0.0, 1.0);
    return normalize(closestPoint);
}

// Roughness modification for area lights (UE4 method)
float getAreaLightRoughness(float roughness, float lightRadius, float lightDistance) {
    float alpha = roughness * roughness;
    float alphaPrime = clamp(alpha + lightRadius / (2.0 * lightDistance), 0.0, 1.0);
    return sqrt(alphaPrime);
}

// ============================================================================
// Subsurface Scattering Approximation (for skin, foliage)
// ============================================================================

vec3 calcSubsurfaceScattering(vec3 L, vec3 V, vec3 N, vec3 albedo, float thickness, float subsurfaceStrength) {
    // Wrap lighting for subsurface effect
    float NdotL = dot(N, L);
    float wrapDiffuse = max(0.0, (NdotL + 0.5) / 1.5);
    
    // Back scattering (light passing through thin surfaces)
    vec3 H = normalize(L + N * 0.5);
    float VdotH = pow(clamp(dot(V, -H), 0.0, 1.0), 3.0);
    float backScatter = VdotH * thickness;
    
    return albedo * (wrapDiffuse + backScatter) * subsurfaceStrength;
}

#endif