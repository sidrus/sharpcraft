#version 450 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D depthTexture;
uniform sampler2D shadowMap;

uniform mat4 invViewProj;
uniform mat4 lightSpaceMatrix;
uniform vec3 lightDir;
uniform vec3 lightColor;
uniform vec3 viewPos;

#include "../Common/math.glsl"
#include "../Common/BRDF.glsl"
#include "../Common/atmosphere.glsl"

uniform int samples = 32;
uniform float scatteringG = 0.8; // Mie anisotropy (higher = more forward scattering)
uniform float densityMultiplier = 0.02; // Base density
uniform float extinctionMultiplier = 0.005;
uniform float rayleighScale = 1.0;
uniform float mieScale = 1.0;
uniform float ozoneScale = 1.0;

float interleavedGradientNoise(vec2 uv) {
    return fract(52.9829189 * fract(dot(uv, vec2(0.06711056, 0.00583715))));
}

void main() {
    float depth = texture(depthTexture, TexCoords).r;
    
    // Reconstruct world position from depth
    vec4 clipPos = vec4(TexCoords * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);
    vec4 viewPosProj = invViewProj * clipPos;
    vec3 worldPos = viewPosProj.xyz / viewPosProj.w;
    
    vec3 rayDir = worldPos - viewPos;
    float rayLength = length(rayDir);
    rayDir /= rayLength;
    
    // Improved dithering for temporal stability
    float jitter = interleavedGradientNoise(gl_FragCoord.xy);
    
    vec3 volumetricColor = vec3(0.0);
    vec3 viewTransmittance = vec3(1.0);
    
    float stepLength = rayLength / float(samples);
    vec3 stepVec = rayDir * stepLength;
    
    vec3 currentPos = viewPos + (stepVec * jitter);
    
    // Light direction (towards the sun)
    vec3 L = -lightDir;
    float sunElevation = L.y;
    
    // Phase functions for view ray
    float cosTheta = dot(rayDir, L);
    float pRayleigh = phaseRayleigh(cosTheta);
    float pMie = phaseMie(cosTheta, scatteringG);
    
    // Time of day factors
    float dayFactor = smoothstep(-0.1, 0.3, sunElevation);
    float nightFactor = 1.0 - smoothstep(-0.35, 0.0, sunElevation);
    float twilightFactor = getTwilightFactor(sunElevation);
    
    // Moon parameters
    vec3 moonDir = -L;
    vec3 moonColor = vec3(0.4, 0.5, 0.8);
    float moonIntensityVal = nightFactor * 0.15;
    
    // Scaled scattering coefficients
    vec3 betaRayleigh = BETA_RAYLEIGH * rayleighScale;
    vec3 betaMie = BETA_MIE * mieScale;

    for (int i = 0; i < samples; i++) {
        float h = max(0.0, currentPos.y);
        
        // Get densities at current altitude
        float dRayleigh = getDensityRayleigh(h);
        float dMie = getDensityMie(h);
        float dOzone = getDensityOzone(h) * ozoneScale;

        // Compute scattering and extinction coefficients
        vec3 sigmaRayleigh = betaRayleigh * dRayleigh * densityMultiplier;
        vec3 sigmaMie = betaMie * dMie * densityMultiplier;
        vec3 sigmaOzone = BETA_OZONE * dOzone * densityMultiplier;
        
        vec3 sigmaScattering = sigmaRayleigh + sigmaMie;
        vec3 sigmaExtinction = sigmaScattering + sigmaOzone * extinctionMultiplier;

        // Shadow map lookup
        vec4 fragPosLightSpace = lightSpaceMatrix * vec4(currentPos, 1.0);
        vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
        projCoords = projCoords * 0.5 + 0.5;
        
        float shadow = 1.0;
        if (projCoords.x >= 0.0 && projCoords.x <= 1.0 && 
            projCoords.y >= 0.0 && projCoords.y <= 1.0 &&
            projCoords.z <= 1.0) {
            float shadowDepth = texture(shadowMap, projCoords.xy).r;
            shadow = (shadowDepth < projCoords.z - 0.002) ? 0.0 : 1.0;
        }
        
        // Sun light contribution
        if (shadow > 0.0 && dayFactor > 0.01) {
            // Transmittance from current point to sun
            vec3 sunTransmittance = getTransmittanceToSun(h, sunElevation);
            
            // Horizon fade for smoother sunset/sunrise
            float horizonFade = smoothstep(-0.05, 0.2, sunElevation);
            sunTransmittance *= shadow * horizonFade;

            // In-scattered light from sun
            vec3 stepScattering = (sigmaRayleigh * pRayleigh + sigmaMie * pMie) * sunTransmittance * lightColor;
            
            volumetricColor += viewTransmittance * stepScattering * stepLength;
        }
        
        // Moonlight contribution (no shadow check for simplicity)
        if (moonIntensityVal > 0.01) {
            float moonElevation = moonDir.y;
            if (moonElevation > -0.1) {
                vec3 moonTransmittance = getTransmittanceToSun(h, moonElevation);
                float moonHorizonFade = smoothstep(-0.1, 0.1, moonElevation);
                moonTransmittance *= moonHorizonFade;
                
                float cosThetaMoon = dot(rayDir, moonDir);
                float pRayleighMoon = phaseRayleigh(cosThetaMoon);
                float pMieMoon = phaseMie(cosThetaMoon, scatteringG * 0.5); // Less forward scattering for moon
                
                vec3 moonScattering = (sigmaRayleigh * pRayleighMoon + sigmaMie * pMieMoon) * moonTransmittance * moonColor * moonIntensityVal;
                volumetricColor += viewTransmittance * moonScattering * stepLength;
            }
        }
        
        // Twilight ambient contribution
        if (twilightFactor > 0.01 && dayFactor < 0.5) {
            vec3 twilightAmbient = vec3(0.3, 0.2, 0.4) * twilightFactor * 0.02;
            volumetricColor += viewTransmittance * twilightAmbient * sigmaScattering * stepLength;
        }
        
        // Update transmittance
        viewTransmittance *= exp(-sigmaExtinction * stepLength);
        currentPos += stepVec;

        // Early exit optimization
        if (max(max(viewTransmittance.r, viewTransmittance.g), viewTransmittance.b) < 0.01) break;
    }
    
    // Output with proper alpha for blending
    float avgTransmittance = (viewTransmittance.r + viewTransmittance.g + viewTransmittance.b) / 3.0;
    FragColor = vec4(volumetricColor, 1.0 - avgTransmittance);
}
