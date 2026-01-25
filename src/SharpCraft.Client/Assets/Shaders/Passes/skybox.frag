#version 450 core
out vec4 FragColor;

in vec3 TexCoords;

layout (std140, binding = 0) uniform SceneData {
    mat4 ViewProjection;
    vec4 ViewPos;
    vec4 FogColor;
    float FogNear;
    float FogFar;
    float Exposure;
    float Gamma;
};

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
    if (sunIntensity <= 0.0 || sunElevation < -0.05) return vec3(0.0);
    
    float cosSun = dot(viewDir, sunDirection);
    
    // Sharp sun disk
    float disk = smoothstep(0.99985, 0.99995, cosSun);
    
    // Sun corona / glow
    float corona = pow(max(cosSun, 0.0), 512.0) * 8.0;
    corona += pow(max(cosSun, 0.0), 64.0) * 0.5;
    
    // Limb darkening for realistic sun disk
    float limbDarkening = 1.0 - pow(1.0 - disk, 0.4) * 0.3;
    
    // Color temperature shift at horizon (redder sun)
    float horizonFactor = smoothstep(0.0, 0.3, sunElevation);
    vec3 diskColor = mix(vec3(1.0, 0.6, 0.3), sunColor, horizonFactor);
    
    vec3 sunDisk = diskColor * disk * sunIntensity * 50.0 * limbDarkening;
    sunDisk += sunColor * corona * sunIntensity * horizonFactor;
    
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
    vec3 V = normalize(TexCoords);
    vec3 sunDirection = -normalize(sunDir);
    float sunElevation = sunDirection.y;
    
    // Determine time of day factors
    float dayFactor = smoothstep(-0.1, 0.3, sunElevation);
    float nightFactor = 1.0 - smoothstep(-0.35, 0.0, sunElevation);
    float twilightFactor = getTwilightFactor(sunElevation);
    int twilightPhase = getTwilightPhase(sunElevation);
    
    // ========================================================================
    // PHYSICALLY-BASED SKY COLOR
    // ========================================================================
    
    vec3 skyTransmittance;
    vec3 skyColor = vec3(0.0);
    
    // Scale the scattering coefficients based on UI parameters
    // (Note: actual scaling would require modifying the atmosphere.glsl uniforms)
    
    if (dayFactor > 0.01 || twilightFactor > 0.01) {
        // Compute physically-based sky scattering
        skyColor = computeSkyColor(V, sunDirection, atmosphereMieG, atmosphereSamples, skyTransmittance);
        
        // Scale by sun intensity and color
        skyColor *= sunIntensity * sunColor * 20.0;
        
        // Add multi-scattering approximation for softer sky
        float viewAltitude = 1.0; // Sea level
        vec3 multiScatter = getMultipleScattering(viewAltitude, sunDirection.y);
        skyColor += multiScatter * sunIntensity * 0.5;
    }
    
    // ========================================================================
    // TWILIGHT ENHANCEMENTS
    // ========================================================================
    
    if (twilightPhase >= 1 && twilightPhase <= 3) {
        // Direction towards sun on horizon plane
        vec3 sunHorizDir = normalize(vec3(sunDirection.x, 0.0, sunDirection.z));
        vec3 viewHorizDir = normalize(vec3(V.x, 0.0, V.z));
        float towardsSun = max(0.0, dot(sunHorizDir, viewHorizDir));
        
        // Height gradient for twilight colors
        float twilightHeight = smoothstep(-0.1, 0.6, V.y);
        
        // Civil twilight: golden hour colors
        if (twilightPhase == 1) {
            vec3 horizonGlow = vec3(1.0, 0.5, 0.2) * (1.0 - twilightHeight) * towardsSun;
            vec3 upperGlow = vec3(0.6, 0.3, 0.5) * twilightHeight;
            skyColor += (horizonGlow + upperGlow) * 0.5 * smoothstep(-0.105, 0.0, sunElevation);
        }
        // Nautical twilight: purple/blue transition
        else if (twilightPhase == 2) {
            vec3 horizonGlow = vec3(0.5, 0.25, 0.3) * (1.0 - twilightHeight) * towardsSun;
            vec3 upperGlow = vec3(0.15, 0.12, 0.3) * twilightHeight;
            skyColor += (horizonGlow + upperGlow) * 0.3;
        }
        // Astronomical twilight: very subtle glow
        else if (twilightPhase == 3) {
            vec3 faintGlow = vec3(0.05, 0.04, 0.1) * (1.0 - twilightHeight) * towardsSun;
            skyColor += faintGlow * 0.2;
        }
        
        // Belt of Venus effect (pink band opposite the sun during twilight)
        float awaySun = max(0.0, -dot(sunHorizDir, viewHorizDir));
        float beltHeight = exp(-V.y * V.y * 8.0) * awaySun;
        vec3 beltColor = vec3(0.7, 0.4, 0.5) * beltHeight * smoothstep(-0.15, 0.0, sunElevation) * 0.3;
        skyColor += beltColor;
    }
    
    // ========================================================================
    // NIGHT SKY
    // ========================================================================
    
    vec3 nightSky = vec3(0.0);
    if (nightFactor > 0.01) {
        // Deep blue-black gradient
        vec3 nightZenith = vec3(0.002, 0.004, 0.012);
        vec3 nightHorizon = vec3(0.008, 0.01, 0.02);
        nightSky = mix(nightHorizon, nightZenith, max(V.y, 0.0));
        
        // Add stars
        nightSky += computeStars(V, nightFactor);
        
        // Add moon
        nightSky += computeMoon(V, nightFactor);
    }
    
    // ========================================================================
    // COMBINE ALL CONTRIBUTIONS
    // ========================================================================
    
    vec3 finalColor = vec3(0.0);
    
    // Blend sky based on time of day
    finalColor += skyColor * (1.0 - nightFactor);
    finalColor += nightSky * nightFactor;
    
    // Add sun disk (always on top)
    finalColor += computeSunDisk(V, sunDirection, sunElevation);
    
    // ========================================================================
    // HORIZON HANDLING
    // ========================================================================
    
    // Smooth transition at horizon to prevent hard line
    float horizonBlend = smoothstep(-0.1, 0.02, V.y);
    
    // Below-horizon color (dark gradient)
    vec3 belowHorizon = mix(
        vec3(0.005, 0.008, 0.015),
        vec3(0.02, 0.025, 0.04),
        twilightFactor
    );
    
    finalColor = mix(belowHorizon, finalColor, horizonBlend);
    
    // Minimum ambient to prevent pure black
    finalColor = max(finalColor, vec3(0.001, 0.0015, 0.003));
    
    // ========================================================================
    // TONE MAPPING (ACES Filmic)
    // ========================================================================
    
    vec3 mapped = finalColor * Exposure;
    
    // ACES filmic tone mapping
    // Based on the ACES curve approximation by Krzysztof Narkowicz
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    mapped = clamp((mapped * (a * mapped + b)) / (mapped * (c * mapped + d) + e), 0.0, 1.0);
    
    // Gamma correction
    mapped = pow(mapped, vec3(1.0 / Gamma));

    FragColor = vec4(mapped, 1.0);
}
