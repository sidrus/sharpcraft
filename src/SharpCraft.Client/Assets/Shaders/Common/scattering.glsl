#ifndef SCATTERING_GLSL
#define SCATTERING_GLSL

#include "math.glsl"
#include "BRDF.glsl"

// Atmosphere constants (Earth-like)
const float EARTH_RADIUS = 6371000.0;
const float ATMOSPHERE_HEIGHT = 100000.0;
const float ATMOSPHERE_RADIUS = EARTH_RADIUS + ATMOSPHERE_HEIGHT;

const vec3 RAYLEIGH_COEFF = vec3(5.802e-6, 13.558e-6, 33.1e-6); // Rayleigh scattering coefficient at sea level
const vec3 MIE_COEFF = vec3(21e-6); // Mie scattering coefficient at sea level

const float RAYLEIGH_SCALE_HEIGHT = 8000.0;
const float MIE_SCALE_HEIGHT = 1200.0;

// Rayleigh phase function
float phaseRayleigh(float cosTheta) {
    return 3.0 / (16.0 * PI) * (1.0 + cosTheta * cosTheta);
}

// Mie phase function (Henyey-Greenstein)
float phaseMie(float cosTheta, float g) {
    float g2 = g * g;
    return 1.0 / (4.0 * PI) * ((1.0 - g2) / pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5));
}

// Optical depth for a simplified atmosphere
float getDensity(float height, float scaleHeight) {
    return exp(-height / scaleHeight);
}

// Improved optical depth with Chapman function approximation for near-horizon transmittance
// This is critical for getting the right colors at sunrise/sunset
float getOpticalDepth(float height, float cosTheta, float scaleHeight) {
    float h = max(0.0, height);
    float r = EARTH_RADIUS + h;
    float y = r / scaleHeight;
    
    // Chapman function approximation
    if (cosTheta >= 0.0) {
        return scaleHeight * exp(-h / scaleHeight) / (cosTheta + 0.15 * pow(cosTheta, 0.1) + 0.05);
    } else {
        // For points below horizon (from POV of light), we use a simpler model or symmetric approach
        return scaleHeight * exp(-h / scaleHeight) * 40.0; // Thick atmosphere at horizon
    }
}

vec3 getAtmosphericTransmittance(float height, float cosTheta) {
    float odRayleigh = getOpticalDepth(height, cosTheta, RAYLEIGH_SCALE_HEIGHT);
    float odMie = getOpticalDepth(height, cosTheta, MIE_SCALE_HEIGHT);
    return exp(-(RAYLEIGH_COEFF * odRayleigh + MIE_COEFF * 1.1 * odMie));
}

#endif