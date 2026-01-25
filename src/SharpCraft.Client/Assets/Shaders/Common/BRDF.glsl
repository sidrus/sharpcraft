#ifndef BRDF_GLSL
#define BRDF_GLSL

#include "math.glsl"

float DistributionGGX(vec3 N, vec3 H, float roughness) {
    float a = roughness*roughness;
    float a2 = a*a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;

    float num = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return num / max(denom, 0.0000001);
}

float GeometrySchlickGGX(float NdotV, float roughness) {
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float num = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return num / max(denom, 0.0000001);
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness) {
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

vec3 fresnelSchlick(float cosTheta, vec3 F0) {
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

vec3 fresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness) {
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

// Improved energy compensation for multi-scattering (Kulla-Conty approximation)
vec3 getEnergyCompensation(float NoV, float roughness, vec3 F0) {
    float a = roughness * roughness;
    vec2 brdf = vec2(NoV, a);
    // Approximate E(mu)
    float ess = 0.5; // Simplified for now, usually fetched from a LUT
    return 1.0 + F0 * (1.0 / ess - 1.0);
}

float computeSpecularAO(float NoV, float ao, float roughness) {
    return clamp(pow(NoV + ao, exp2(-16.0 * roughness - 1.0)) - 1.0 + ao, 0.0, 1.0);
}

// ============================================================================
// Tone Mapping Functions
// ============================================================================

// Valve's cinematic tonemapping (legacy)
vec3 ToneMap_Filmic(vec3 x) {
    vec3 X = max(vec3(0.0), x - 0.004);
    vec3 result = (X * (6.2 * X + 0.5)) / (X * (6.2 * X + 1.7) + 0.06);
    return pow(result, vec3(2.2));
}

// ACES Filmic Tone Mapping (Narkowicz approximation)
// Industry standard, used in UE4/5, Unity HDRP
vec3 ToneMap_ACES(vec3 x) {
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

// ACES with proper RRT and ODT matrices (more accurate)
vec3 ToneMap_ACES_Fitted(vec3 color) {
    // sRGB => XYZ => D65_2_D60 => AP1 => RRT_SAT
    const mat3 ACESInputMat = mat3(
        0.59719, 0.07600, 0.02840,
        0.35458, 0.90834, 0.13383,
        0.04823, 0.01566, 0.83777
    );
    
    // ODT_SAT => XYZ => D60_2_D65 => sRGB
    const mat3 ACESOutputMat = mat3(
        1.60475, -0.10208, -0.00327,
        -0.53108, 1.10813, -0.07276,
        -0.07367, -0.00605, 1.07602
    );
    
    color = ACESInputMat * color;
    
    // Apply RRT and ODT
    vec3 a = color * (color + 0.0245786) - 0.000090537;
    vec3 b = color * (0.983729 * color + 0.4329510) + 0.238081;
    color = a / b;
    
    color = ACESOutputMat * color;
    
    return clamp(color, 0.0, 1.0);
}

// Reinhard tone mapping (simple, good for comparison)
vec3 ToneMap_Reinhard(vec3 x) {
    return x / (x + vec3(1.0));
}

// Uncharted 2 filmic (legacy game standard)
vec3 ToneMap_Uncharted2(vec3 x) {
    const float A = 0.15;
    const float B = 0.50;
    const float C = 0.10;
    const float D = 0.20;
    const float E = 0.02;
    const float F = 0.30;
    return ((x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E / F;
}

// ============================================================================
// Improved Multi-Scattering GGX (Kulla-Conty)
// ============================================================================

// Precomputed directional albedo approximation
float getDirectionalAlbedo(float NoV, float roughness) {
    // Polynomial approximation of the directional albedo integral
    float a = roughness;
    float a2 = a * a;
    
    // Approximation based on curve fitting
    float f0 = 1.0 - a;
    float f1 = 1.0 - NoV;
    float f2 = f1 * f1;
    
    return f0 * (1.0 - 0.28 * a2 * f2 * f1) + 0.28 * a2 * f2;
}

// Multi-scattering energy compensation term
vec3 getMultiScatteringCompensation(float NoV, float NoL, float roughness, vec3 F0) {
    float Eo = getDirectionalAlbedo(NoV, roughness);
    float Ei = getDirectionalAlbedo(NoL, roughness);
    
    // Average Fresnel
    vec3 Favg = F0 + (1.0 - F0) / 21.0;
    
    // Multi-scatter contribution
    float Eavg = 1.0 - (1.0 - Eo) * (1.0 - Ei);
    vec3 fms = Favg * Favg * Eavg / (1.0 - Favg * (1.0 - Eavg));
    
    return fms;
}

#endif