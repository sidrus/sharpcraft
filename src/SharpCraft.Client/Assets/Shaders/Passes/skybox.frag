#version 450 core
out vec4 FragColor;

in vec2 Ndc;

layout (std140, binding = 0) uniform SceneData {
    mat4 ViewProjection;
    vec4 ViewPos;
    vec4 FogColor;
    float FogNear;
    float FogFar;
    float Exposure;
    float Gamma;
};

// Inverse of the (jittered) view-projection, for exact per-pixel sky ray reconstruction.
uniform mat4 InvViewProj;

// Sun parameters
uniform vec3 sunDir;
uniform vec3 sunColor;
uniform float sunIntensity;

// Moon parameters
uniform vec3 moonDir;
uniform vec3 moonColor;
uniform float moonIntensity;

// Atmosphere parameters (controllable from UI)
uniform float atmosphereMieG = 0.8;
uniform float atmosphereRayleighScale = 1.0;
uniform float atmosphereMieScale = 1.0;
uniform float atmosphereOzoneScale = 1.0;
uniform int atmosphereSamples = 16;

#include "../Common/math.glsl"
#include "../Common/atmosphere.glsl"

// ============================================================================
// Stars
// ============================================================================

vec3 computeStars(vec3 viewDir, float nightFactor) {
    if (nightFactor < 0.01) return vec3(0.0);
    
    // Multi-layer star field for depth
    vec3 stars = vec3(0.0);
    
    // Layer 1: Bright stars (sparse)
    float starNoise1 = fract(sin(dot(viewDir, vec3(12.9898, 78.233, 45.164))) * 43758.5453);
    if (starNoise1 > 0.9997) {
        float brightness = (starNoise1 - 0.9997) / 0.0003;
        // Star color temperature variation
        vec3 starColor = mix(vec3(0.8, 0.9, 1.0), vec3(1.0, 0.95, 0.8), fract(starNoise1 * 127.3));
        stars += starColor * brightness * 2.0;
    }
    
    // Layer 2: Medium stars
    float starNoise2 = fract(sin(dot(viewDir, vec3(93.989, 67.345, 23.654))) * 23421.631);
    if (starNoise2 > 0.999) {
        float brightness = (starNoise2 - 0.999) / 0.001;
        stars += vec3(0.7, 0.8, 0.9) * brightness * 0.8;
    }
    
    // Layer 3: Faint stars (dense)
    float starNoise3 = fract(sin(dot(viewDir, vec3(34.534, 12.876, 89.123))) * 65432.123);
    if (starNoise3 > 0.998) {
        float brightness = (starNoise3 - 0.998) / 0.002;
        stars += vec3(0.5, 0.55, 0.6) * brightness * 0.3;
    }
    
    // Twinkling effect (subtle)
    float twinkle = 0.9 + 0.1 * sin(starNoise1 * 1000.0 + FogNear * 0.001); // Use FogNear as pseudo-time
    
    return stars * nightFactor * twinkle;
}

// ============================================================================
// Sun Disk and Corona
// ============================================================================

vec3 computeSunDisk(vec3 viewDir, vec3 sunDirection, float sunElevation) {
    // Show sun disk when sun is visible (above horizon)
    // Don't use sunIntensity here - that's for direct lighting, not visibility
    if (sunElevation < -0.02) return vec3(0.0); // Hide when clearly below horizon
    
    float cosSun = dot(viewDir, sunDirection);
    
    // Sharp sun disk
    float disk = smoothstep(0.99985, 0.99995, cosSun);
    
    // Sun corona / glow
    float corona = pow(max(cosSun, 0.0), 512.0) * 8.0;
    corona += pow(max(cosSun, 0.0), 64.0) * 0.5;
    
    // Limb darkening for realistic sun disk
    float limbDarkening = 1.0 - pow(1.0 - disk, 0.4) * 0.3;
    
    // Color temperature shift at horizon (redder sun at low angles)
    float horizonFactor = smoothstep(-0.02, 0.3, sunElevation);
    vec3 diskColor = mix(vec3(1.0, 0.4, 0.1), sunColor, horizonFactor); // More orange/red at horizon
    
    // Sun brightness based on elevation (dimmer at horizon due to atmospheric extinction)
    float sunVisibility = smoothstep(-0.02, 0.1, sunElevation);
    
    // Atmospheric extinction makes sun dimmer and redder at horizon
    vec3 extinction = exp(-vec3(0.5, 1.0, 2.0) * (1.0 - sunElevation) * 3.0);
    
    vec3 sunDisk = diskColor * disk * 40.0 * limbDarkening * sunVisibility * extinction;
    sunDisk += diskColor * corona * sunVisibility * extinction * 0.5;
    
    return sunDisk;
}

// ============================================================================
// Moon
// ============================================================================

vec3 computeMoon(vec3 viewDir, float nightFactor) {
    if (moonIntensity <= 0.0 || nightFactor < 0.1) return vec3(0.0);
    
    vec3 moonDirection = -normalize(moonDir);
    float cosMoon = dot(viewDir, moonDirection);
    
    // Moon disk (larger than sun visually)
    float disk = smoothstep(0.9995, 0.9999, cosMoon);
    
    // Subtle moon glow
    float glow = pow(max(cosMoon, 0.0), 32.0) * 0.15;
    
    vec3 moon = moonColor * disk * moonIntensity * 5.0;
    moon += moonColor * glow * moonIntensity * nightFactor;
    
    return moon;
}

// ============================================================================
// Main Sky Rendering
// ============================================================================

void main()
{
    // Exact per-pixel view ray (any depth along the ray gives the same direction).
    vec4 worldH = InvViewProj * vec4(Ndc, 0.5, 1.0);
    vec3 V = normalize(worldH.xyz / worldH.w - ViewPos.xyz);
    vec3 sunDirection = -normalize(sunDir);
    float sunElevation = sunDirection.y;

    // Physical single-scattering sky (warm horizon, blue zenith and blue-hour are intrinsic — no
    // magic boosts or twilight-phase branches). Dims naturally as the sun sets.
    vec3 sky = skyRadiance(V, sunDirection, sunColor, atmosphereMieG, atmosphereSamples);

    // Night sky: fade in a dark gradient + stars + moon as the sun drops below the horizon.
    float nightFactor = 1.0 - smoothstep(-0.14, 0.0, sunElevation);
    if (nightFactor > 0.001) {
        vec3 nightZenith = vec3(0.002, 0.004, 0.012);
        vec3 nightHorizon = vec3(0.006, 0.008, 0.018);
        vec3 nightSky = mix(nightHorizon, nightZenith, clamp(V.y, 0.0, 1.0));
        nightSky += computeStars(V, nightFactor);
        nightSky += computeMoon(V, nightFactor);
        sky = mix(sky, nightSky, nightFactor);
    }

    // The sun disc itself is drawn by SunRenderer (with horizon clipping), so it isn't added here.
    // Gentle floor so the sky is never pure black.
    sky = max(sky, vec3(0.001, 0.0015, 0.003));

    // Output true HDR radiance — exposure + tonemap are applied in the post-process pass (fxaa.frag, §5.2)
    FragColor = vec4(sky, 1.0);
}
