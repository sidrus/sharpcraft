#ifndef ATMOSPHERE_GLSL
#define ATMOSPHERE_GLSL

#include "math.glsl"

// ============================================================================
// Physically-Based Atmospheric Scattering
// Based on Bruneton & Neyret's precomputed atmospheric scattering model
// with real-time approximations suitable for games
// ============================================================================

// Physical constants for Earth-like atmosphere
const float PLANET_RADIUS = 6371000.0;          // Earth radius in meters
const float ATMOSPHERE_HEIGHT = 100000.0;        // 100km atmosphere
const float ATMOSPHERE_RADIUS = PLANET_RADIUS + ATMOSPHERE_HEIGHT;

// Scale heights (how quickly density decreases with altitude)
const float H_RAYLEIGH = 8500.0;   // Rayleigh scale height (8.5km)
const float H_MIE = 1200.0;        // Mie scale height (1.2km)  
const float H_OZONE = 25000.0;     // Ozone layer center height

// Scattering coefficients at sea level (per meter)
// These are wavelength-dependent for RGB (680nm, 550nm, 440nm)
const vec3 BETA_RAYLEIGH = vec3(5.802e-6, 13.558e-6, 33.1e-6);
const vec3 BETA_MIE = vec3(3.996e-6);  // Mie is mostly wavelength-independent

// Ozone absorption coefficients (critical for blue hour / twilight colors)
// Ozone absorbs orange/red light, giving the deep blue twilight sky
const vec3 BETA_OZONE = vec3(0.650e-6, 1.881e-6, 0.085e-6);

// Mie extinction is slightly higher than scattering due to absorption
const float MIE_ABSORPTION_RATIO = 1.11;

// Default Mie anisotropy (forward scattering bias)
const float DEFAULT_MIE_G = 0.8;

// Multiple-scattering fill (cheap Hillaire-style approximation). MS_REF_ALTITUDE is the reference
// height the isotropic fill samples the sun transmittance at — high enough that blue light still
// survives at low sun, which is what keeps the twilight zenith blue instead of brown. MS_STRENGTH
// scales the isotropic contribution; raise it for a bluer/brighter sky, lower it toward 0 to revert
// to pure single scattering.
const float MS_REF_ALTITUDE = 15000.0;
const float MS_STRENGTH = 0.12;

// ============================================================================
// Density Functions
// ============================================================================

// Rayleigh density at given altitude
float getDensityRayleigh(float altitude) {
    return exp(-altitude / H_RAYLEIGH);
}

// Mie density at given altitude  
float getDensityMie(float altitude) {
    return exp(-altitude / H_MIE);
}

// Ozone density follows a peaked distribution around 25km altitude
// This is crucial for proper twilight/blue hour colors
float getDensityOzone(float altitude) {
    // Ozone layer peaks around 25km with ~15km thickness
    float ozonePeak = 25000.0;
    float ozoneWidth = 15000.0;
    return max(0.0, 1.0 - abs(altitude - ozonePeak) / ozoneWidth);
}

// ============================================================================
// Phase Functions
// ============================================================================

// Rayleigh phase function - symmetric scattering for small particles
float phaseRayleigh(float cosTheta) {
    return (3.0 / (16.0 * PI)) * (1.0 + cosTheta * cosTheta);
}

// Henyey-Greenstein phase function for Mie scattering
// g > 0 means forward scattering (typical for aerosols)
float phaseMie(float cosTheta, float g) {
    float g2 = g * g;
    float denom = 1.0 + g2 - 2.0 * g * cosTheta;
    return (1.0 / (4.0 * PI)) * ((1.0 - g2) / (denom * sqrt(max(denom, 1e-6))));
}

// Cornette-Shanks phase function (improved Mie for aerosols)
// Better approximation of real aerosol scattering
float phaseCornetteShank(float cosTheta, float g) {
    float g2 = g * g;
    float num = 3.0 * (1.0 - g2) * (1.0 + cosTheta * cosTheta);
    float denom = (8.0 * PI) * (2.0 + g2) * pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5);
    return num / max(denom, 1e-6);
}

// ============================================================================
// Ray-Sphere Intersection
// ============================================================================

// Returns distance to sphere intersection (or -1 if no intersection)
// origin: ray origin, dir: normalized ray direction, radius: sphere radius
vec2 raySphereIntersect(vec3 origin, vec3 dir, float radius) {
    float a = dot(dir, dir);
    float b = 2.0 * dot(dir, origin);
    float c = dot(origin, origin) - radius * radius;
    float d = b * b - 4.0 * a * c;
    
    if (d < 0.0) return vec2(-1.0);
    
    d = sqrt(d);
    return vec2(-b - d, -b + d) / (2.0 * a);
}

// ============================================================================
// Optical Depth Calculation
// ============================================================================

// Compute optical depth along a ray through the atmosphere
// This integrates the density from the starting point to the edge of atmosphere
vec3 computeOpticalDepth(vec3 rayOrigin, vec3 rayDir, float rayLength, int numSamples) {
    float stepSize = rayLength / float(numSamples);
    vec3 opticalDepth = vec3(0.0);
    
    for (int i = 0; i < numSamples; i++) {
        vec3 samplePos = rayOrigin + rayDir * (float(i) + 0.5) * stepSize;
        float altitude = length(samplePos) - PLANET_RADIUS;
        
        if (altitude < 0.0) break;
        
        float densityR = getDensityRayleigh(altitude);
        float densityM = getDensityMie(altitude);
        float densityO = getDensityOzone(altitude);
        
        opticalDepth.x += densityR;
        opticalDepth.y += densityM;
        opticalDepth.z += densityO;
    }
    
    return opticalDepth * stepSize;
}

// ============================================================================
// Transmittance (how much light is absorbed/scattered out)
// ============================================================================

vec3 computeTransmittance(vec3 opticalDepth) {
    return exp(-(
        BETA_RAYLEIGH * opticalDepth.x +
        BETA_MIE * MIE_ABSORPTION_RATIO * opticalDepth.y +
        BETA_OZONE * opticalDepth.z
    ));
}

// Transmittance for a ray from a sample point toward the sun.
//
// This is computed by *marching the sun ray* and accumulating optical depth, rather than the older
// two-branch Chapman approximation. The Chapman version used one formula for cosZenith >= 0 and a
// different grazing formula for cosZenith < 0; the two did not agree at the horizon, so both the
// optical-depth term and the planet-shadow factor jumped discontinuously at cosZenith = 0. Sampled
// across the sky (where the sun-zenith crosses zero along a line) that drew a hard diagonal seam —
// the artifact visible when facing away from the sun. A march is continuous by construction, so no
// seam can form, at the cost of a few extra density evaluations (cheap for a once-per-frame sky).
vec3 getTransmittanceToSun(float altitude, float cosZenith) {
    altitude = max(altitude, 0.0);

    // Reconstruct the sample position and sun-ray direction in planet space. Only the altitude
    // profile along the ray matters, so the horizontal axis is arbitrary.
    vec3 pos = vec3(0.0, PLANET_RADIUS + altitude, 0.0);
    float sinZenith = sqrt(max(1.0 - cosZenith * cosZenith, 0.0));
    vec3 dir = vec3(sinZenith, cosZenith, 0.0);

    // March from the sample point to the top of the atmosphere (or until the ray dips underground,
    // when the sun is below the local horizon).
    vec2 atmHit = raySphereIntersect(pos, dir, ATMOSPHERE_RADIUS);
    float rayLen = max(atmHit.y, 0.0);

    const int SUN_STEPS = 6;
    float stepSize = rayLen / float(SUN_STEPS);
    vec3 od = vec3(0.0);
    for (int i = 0; i < SUN_STEPS; i++) {
        vec3 sp = pos + dir * (float(i) + 0.5) * stepSize;
        float alt = length(sp) - PLANET_RADIUS;
        if (alt < 0.0) break; // ray has gone below the surface
        od += vec3(getDensityRayleigh(alt), getDensityMie(alt), getDensityOzone(alt) * 0.3) * stepSize;
    }

    vec3 transmittance = exp(-(
        BETA_RAYLEIGH * od.x +
        BETA_MIE * MIE_ABSORPTION_RATIO * od.y +
        BETA_OZONE * od.z
    ));

    // Soft planet-shadow terminator, applied as a single smoothstep across the horizon (continuous
    // through cosZenith = 0, where it is 1.0 on both sides). Fades the sun's contribution to zero
    // over a broad band as it drops below the local horizon, so the Earth-shadow boundary reads as a
    // smooth twilight gradient instead of a seam.
    float shadow = smoothstep(-0.1, 0.0, cosZenith);
    return transmittance * shadow;
}

// ============================================================================
// Sky Color Computation
// ============================================================================

// Compute single-scattered sky color along view direction
vec3 computeSkyColor(
    vec3 viewDir,
    vec3 sunDir,
    float mieG,
    int numSamples,
    out vec3 transmittance
) {
    // Camera at sea level (or slightly above)
    vec3 rayOrigin = vec3(0.0, PLANET_RADIUS + 1.0, 0.0);
    
    // Find intersection with atmosphere
    vec2 atmosphereHit = raySphereIntersect(rayOrigin, viewDir, ATMOSPHERE_RADIUS);
    if (atmosphereHit.y < 0.0) {
        transmittance = vec3(1.0);
        return vec3(0.0);
    }
    
    // Check for planet intersection
    vec2 planetHit = raySphereIntersect(rayOrigin, viewDir, PLANET_RADIUS);
    float rayLength = (planetHit.x > 0.0) ? planetHit.x : atmosphereHit.y;
    
    float stepSize = rayLength / float(numSamples);
    
    vec3 scatteringR = vec3(0.0);
    vec3 scatteringM = vec3(0.0);
    vec3 multiScatter = vec3(0.0); // isotropic multiple-scattering fill (see below)
    vec3 opticalDepth = vec3(0.0);

    float cosTheta = dot(viewDir, sunDir);
    float phaseR = phaseRayleigh(cosTheta);
    float phaseM = phaseMie(cosTheta, mieG);

    for (int i = 0; i < numSamples; i++) {
        vec3 samplePos = rayOrigin + viewDir * (float(i) + 0.5) * stepSize;
        float altitude = length(samplePos) - PLANET_RADIUS;

        if (altitude < 0.0) break;

        // Local density
        float densityR = getDensityRayleigh(altitude);
        float densityM = getDensityMie(altitude);
        float densityO = getDensityOzone(altitude);

        // Accumulate optical depth along view ray
        vec3 localOD = vec3(densityR, densityM, densityO) * stepSize;
        opticalDepth += localOD;

        // Transmittance from camera to sample point
        vec3 viewTransmittance = computeTransmittance(opticalDepth);

        // Transmittance from sample point to sun
        float sunCosZenith = dot(normalize(samplePos), sunDir);
        vec3 sunTransmittance = getTransmittanceToSun(altitude, sunCosZenith);

        // Combined transmittance
        vec3 totalTransmittance = viewTransmittance * sunTransmittance;

        // Accumulate in-scattered (single-scattered) light
        scatteringR += totalTransmittance * densityR * stepSize;
        scatteringM += totalTransmittance * densityM * stepSize;

        // Multiple-scattering fill (isotropic). Single scattering alone loses ALL blue at low sun:
        // the long ground-level path to the sun extincts short wavelengths first, so the twilight
        // zenith collapses to brown/red. In reality the dominant twilight skylight has bounced
        // multiple times and originates high in the atmosphere, where blue is not yet extinct. We
        // approximate that by lighting an isotropic Rayleigh term with the sun transmittance taken at
        // a high reference altitude (MS_REF_ALTITUDE) instead of the sample's own altitude — so blue
        // survives and the zenith stays blue through twilight. (Hillaire-style multiple scattering.)
        vec3 msSunTransmittance = getTransmittanceToSun(MS_REF_ALTITUDE, sunCosZenith);
        multiScatter += viewTransmittance * msSunTransmittance * densityR * stepSize;
    }

    transmittance = computeTransmittance(opticalDepth);

    // Final scattered light: single scattering (phase-weighted) + isotropic multiple-scattering fill.
    vec3 skyColor = scatteringR * BETA_RAYLEIGH * phaseR +
                    scatteringM * BETA_MIE * phaseM +
                    multiScatter * BETA_RAYLEIGH * MS_STRENGTH;

    return skyColor;
}

// ============================================================================
// Clean sky radiance — single entry point for the visible sky AND the IBL capture, so reflections
// match the sky. Just the physical single-scattering, scaled by one sane illuminance constant and
// tinted by the sun color. No magic horizon boosts / twilight-phase hacks: the long atmospheric
// path at low sun reddens the horizon and ozone gives the blue hour for free. Day→twilight→night
// dimming is intrinsic (sun transmittance drops as the sun sets).
// ============================================================================
const float SKY_ILLUMINANCE = 38.0;

vec3 skyRadiance(vec3 viewDir, vec3 sunDirection, vec3 sunColor, float mieG, int samples) {
    vec3 transmittance;
    vec3 sky = computeSkyColor(viewDir, sunDirection, mieG, samples, transmittance);
    return max(sky, vec3(0.0)) * SKY_ILLUMINANCE * sunColor;
}

// ============================================================================
// Multi-Scattering Approximation
// ============================================================================

// Approximate multiple scattering contribution (Hillaire's method)
// This adds the soft ambient light from light bouncing multiple times
vec3 getMultipleScattering(float altitude, float cosZenith) {
    // Multi-scattering is roughly isotropic and adds ambient light
    vec3 sunTransmittance = getTransmittanceToSun(altitude, cosZenith);
    
    // Approximate multiple scattering as fraction of single scattering
    // This is a simplification of the full integral
    float multiScatterFactor = 0.3;
    
    vec3 multiScatter = (BETA_RAYLEIGH * getDensityRayleigh(altitude) + 
                         BETA_MIE * getDensityMie(altitude)) * 
                        sunTransmittance * multiScatterFactor;
    
    return multiScatter;
}

// ============================================================================
// Twilight Phase Detection
// ============================================================================

// Returns: 0 = day, 1 = civil twilight, 2 = nautical, 3 = astronomical, 4 = night
int getTwilightPhase(float sunElevation) {
    // sunElevation is sin of solar elevation angle
    // Civil: 0° to -6° (-0.105 radians)
    // Nautical: -6° to -12° (-0.208 radians)  
    // Astronomical: -12° to -18° (-0.309 radians)
    
    if (sunElevation > 0.0) return 0;           // Day
    if (sunElevation > -0.105) return 1;        // Civil twilight
    if (sunElevation > -0.208) return 2;        // Nautical twilight
    if (sunElevation > -0.309) return 3;        // Astronomical twilight
    return 4;                                    // Night
}

// Get twilight factor for blending (0 = full night, 1 = full day)
float getTwilightFactor(float sunElevation) {
    // Smooth transition through twilight phases
    if (sunElevation > 0.1) return 1.0;
    if (sunElevation < -0.35) return 0.0;
    
    // Civil: bright, can still read
    float civil = smoothstep(-0.105, 0.05, sunElevation) * 0.7;
    // Nautical: horizon barely visible
    float nautical = smoothstep(-0.208, -0.08, sunElevation) * 0.2;
    // Astronomical: almost dark
    float astronomical = smoothstep(-0.35, -0.18, sunElevation) * 0.1;
    
    return civil + nautical + astronomical;
}

// ============================================================================
// Aerial Perspective (Fog/Haze for Distant Objects)
// ============================================================================

// Compute the atmospheric color and transmittance between two points
// Used for rendering distant terrain with proper atmospheric perspective
void computeAerialPerspective(
    vec3 startPos,
    vec3 endPos,
    vec3 sunDir,
    float mieG,
    out vec3 inscatter,
    out vec3 transmittance
) {
    vec3 rayDir = endPos - startPos;
    float rayLength = length(rayDir);
    rayDir /= rayLength;
    
    int numSamples = 8; // Lower samples for performance
    float stepSize = rayLength / float(numSamples);
    
    inscatter = vec3(0.0);
    vec3 opticalDepth = vec3(0.0);
    
    float cosTheta = dot(rayDir, sunDir);
    float phaseR = phaseRayleigh(cosTheta);
    float phaseM = phaseMie(cosTheta, mieG);
    
    for (int i = 0; i < numSamples; i++) {
        vec3 samplePos = startPos + rayDir * (float(i) + 0.5) * stepSize;
        float altitude = max(0.0, samplePos.y); // Simplified: assume flat terrain
        
        float densityR = getDensityRayleigh(altitude);
        float densityM = getDensityMie(altitude);
        float densityO = getDensityOzone(altitude);
        
        vec3 localOD = vec3(densityR, densityM, densityO) * stepSize;
        opticalDepth += localOD;
        
        vec3 viewTransmittance = computeTransmittance(opticalDepth);
        
        float sunCosZenith = sunDir.y;
        vec3 sunTransmittance = getTransmittanceToSun(altitude, sunCosZenith);
        
        vec3 totalTransmittance = viewTransmittance * sunTransmittance;
        
        inscatter += totalTransmittance * (
            BETA_RAYLEIGH * densityR * phaseR +
            BETA_MIE * densityM * phaseM
        ) * stepSize;
    }
    
    transmittance = computeTransmittance(opticalDepth);
}

#endif
