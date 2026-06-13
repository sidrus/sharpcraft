#version 450 core
out vec4 FragColor;

in vec3 LocalPos;

// Direction TO the sun (normalized), sun tint, Mie anisotropy, and a capture intensity that
// keeps the integrated ambient from washing out (we have no auto-exposure yet).
uniform vec3 sunDir;
uniform vec3 sunColor;
uniform float mieG;
uniform float captureIntensity;

#include "../Common/math.glsl"
#include "../Common/atmosphere.glsl"

// Captures the procedural sky dome radiance for IBL (research §4.2). Mirrors the visible
// skybox's core scattering, minus the sun disk / stars / twilight extras that would inject
// fireflies into the irradiance and prefilter integrals.
void main() {
    vec3 V = normalize(LocalPos);
    vec3 sunDirection = normalize(sunDir);
    float sunElevation = sunDirection.y;

    float skyBrightness = smoothstep(-0.15, 0.3, sunElevation);

    vec3 transmittance;
    vec3 skyColor = vec3(0.0);
    if (sunElevation > -0.26) {
        skyColor = computeSkyColor(V, sunDirection, mieG, 16, transmittance);
        float horizonBoost = 1.0 + (1.0 - abs(sunElevation)) * 2.0;
        skyColor *= skyBrightness * sunColor * captureIntensity * horizonBoost;

        vec3 multiScatter = getMultipleScattering(1.0, sunDirection.y);
        skyColor += multiScatter * skyBrightness * 0.8;
    }

    // Night floor so shadowed/ambient never reaches pure black.
    skyColor = max(skyColor, vec3(0.002, 0.004, 0.012));

    FragColor = vec4(skyColor, 1.0);
}
