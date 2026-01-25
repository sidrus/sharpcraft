#version 450 core

out vec4 FragColor;

in vec2 TexCoords;

// G-Buffer textures
uniform sampler2D gAlbedoAO;
uniform sampler2D gNormal;
uniform sampler2D gMaterial;
uniform sampler2D gPosition;
uniform sampler2D gDepth;

// IBL textures
uniform samplerCube irradianceMap;
uniform samplerCube prefilterMap;
uniform sampler2D brdfLUT;
uniform bool useIBL;

// Shadow map
uniform sampler2DShadow shadowMap;

// Tone mapping selection (0=ACES, 1=Filmic, 2=Reinhard)
uniform int toneMapMode = 0;

layout (std140, binding = 0) uniform SceneData {
    mat4 ViewProjection;
    vec4 ViewPos;
    vec4 FogColor;
    float FogNear;
    float FogFar;
    float Exposure;
    float Gamma;
};

struct DirLight {
    vec4 direction;
    vec4 color;
};

struct PointLight {
    vec4 position;
    vec4 color;
    float intensity;
    float constant;
    float linear;
    float quadratic;
};

layout (std140, binding = 1) uniform LightingData {
    mat4 LightSpaceMatrix;
    DirLight dirLight;
    PointLight pointLights[4];
};

#include "../Common/math.glsl"
#include "../Common/BRDF.glsl"
#include "../Common/atmosphere.glsl"
#include "../Common/lighting.glsl"
#include "../Common/shadows.glsl"

void main() {
    // Sample G-Buffer
    vec4 albedoAO = texture(gAlbedoAO, TexCoords);
    vec4 normalEncoded = texture(gNormal, TexCoords);
    vec4 material = texture(gMaterial, TexCoords);
    vec4 positionDist = texture(gPosition, TexCoords);
    
    // Early out for skybox pixels (no geometry)
    if (positionDist.w <= 0.0) {
        discard;
    }
    
    // Decode G-Buffer data
    vec3 albedo = albedoAO.rgb;
    float ao = albedoAO.a;
    vec3 norm = normalize(normalEncoded.rgb * 2.0 - 1.0);  // Decode normal from [0,1] to [-1,1]
    float metallic = material.r;
    float roughness = material.g;
    vec3 FragPos = positionDist.xyz;
    float FragDistance = positionDist.w;
    
    vec3 V = normalize(ViewPos.xyz - FragPos);
    
    vec3 F0 = vec3(0.04); 
    F0 = mix(F0, albedo, metallic);

    // Lighting Accumulation
    vec3 Lo = vec3(0.0);

    // Calculate shadow
    vec4 fragPosLightSpace = LightSpaceMatrix * vec4(FragPos, 1.0);
    float shadow = 0.0;
    if (length(dirLight.color.xyz) > 0.0) {
        shadow = CalcShadow(shadowMap, fragPosLightSpace, norm, normalize(-dirLight.direction.xyz));
    }

    // Add Directional Light (Sun)
    if (length(dirLight.color.xyz) > 0.0) {
        vec3 L = normalize(-dirLight.direction.xyz);
        Lo += (1.0 - shadow) * CalcPBRLighting(L, V, norm, F0, albedo, metallic, roughness, dirLight.color.xyz);
    }
    
    // Add Moonlight (simulated)
    vec3 moonDir = normalize(dirLight.direction.xyz);
    float sunVisible = clamp(dot(normalize(-dirLight.direction.xyz), vec3(0, 1, 0)) * 5.0, 0.0, 1.0);
    float moonIntensity = (1.0 - sunVisible) * 0.1;
    vec3 moonColor = vec3(0.5, 0.6, 1.0) * moonIntensity;
    
    if (moonIntensity > 0.0) {
        Lo += CalcPBRLighting(moonDir, V, norm, F0, albedo, metallic, roughness, moonColor);
    }

    // Add Point Lights
    for(int i = 0; i < 4; i++) {
        if (pointLights[i].intensity > 0.0) {
            vec3 L = normalize(pointLights[i].position.xyz - FragPos);
            float distance = length(pointLights[i].position.xyz - FragPos);
            float attenuation = 1.0 / (pointLights[i].constant + pointLights[i].linear * distance + pointLights[i].quadratic * (distance * distance));
            vec3 radiance = pointLights[i].color.xyz * pointLights[i].intensity * attenuation;
            
            Lo += CalcPBRLighting(L, V, norm, F0, albedo, metallic, roughness, radiance);
        }
    }

    // Ambient/IBL
    vec3 ambient;
    
    // Calculate sky-based ambient that works during twilight
    // This provides fill light even when direct sun intensity is low
    vec3 skyAmbientDir = normalize(-dirLight.direction.xyz);
    float skyAmbientHeight = skyAmbientDir.y;
    
    // Sky ambient brightness based on sun elevation (not sunIntensity)
    // Provides light during civil twilight (-6° to 0°) and beyond
    float skyAmbientFactor = smoothstep(-0.15, 0.4, skyAmbientHeight);
    
    // Twilight ambient color (warm near horizon during sunrise/sunset)
    vec3 twilightAmbientColor = mix(
        vec3(0.4, 0.25, 0.15), // Warm twilight
        vec3(0.6, 0.7, 1.0),   // Blue sky
        smoothstep(0.0, 0.3, skyAmbientHeight)
    );
    
    // Minimum ambient for night (moonlight + starlight)
    vec3 nightAmbient = vec3(0.015, 0.02, 0.035) * albedo * ao;
    
    if (useIBL) {
        float NoV = max(dot(norm, V), 0.0);
        vec3 F = fresnelSchlickRoughness(NoV, F0, roughness);
        vec3 kS = F;
        vec3 kD = 1.0 - kS;
        kD *= 1.0 - metallic;
        
        vec3 irradiance = texture(irradianceMap, norm).rgb;
        vec3 diffuse = irradiance * albedo;
        
        const float MAX_REFLECTION_LOD = 4.0;
        vec3 R = reflect(-V, norm);
        vec3 prefilteredColor = textureLod(prefilterMap, R, roughness * MAX_REFLECTION_LOD).rgb;
        vec2 envBRDF = texture(brdfLUT, vec2(NoV, roughness)).rg;
        vec3 specular = prefilteredColor * (F * envBRDF.x + envBRDF.y);
        
        // Specular AO
        specular *= computeSpecularAO(NoV, ao, roughness);
        
        ambient = (kD * diffuse + specular) * ao;
    } else {
        // Sky-based ambient when IBL is not available
        vec3 skyAmbient = twilightAmbientColor * skyAmbientFactor * 0.15 * albedo * ao;
        ambient = max(skyAmbient, nightAmbient);
    }
    
    vec3 result = ambient + Lo;

    // === PHYSICALLY-BASED ATMOSPHERIC FOG ===
    vec3 sunDirection = normalize(-dirLight.direction.xyz);
    float sunHeight = sunDirection.y; // This is sin(elevation angle)
    
    // Time-of-day factors matching skybox
    // Use wider range for dayFactor to include sunrise/sunset
    float dayFactor = smoothstep(-0.15, 0.3, sunHeight);
    float nightFactor = 1.0 - smoothstep(-0.35, 0.0, sunHeight);
    float twilightFactor = getTwilightFactor(sunHeight);
    int twilightPhase = getTwilightPhase(sunHeight);
    
    // Compute aerial perspective (inscattered light along view ray)
    vec3 viewDir = normalize(FragPos - ViewPos.xyz);
    float viewAltitude = max(0.0, ViewPos.y);
    
    // Simplified aerial perspective based on distance
    float fogDistance = FogFar - FogNear;
    float fogFactor = clamp((FragDistance - FogNear) / max(fogDistance, 0.0001), 0.0, 1.0);
    fogFactor = pow(fogFactor, 1.3); // Slightly less aggressive falloff
    
    // Compute fog color based on time of day and view direction
    vec3 inscatteredLight = vec3(0.0);
    
    if (dayFactor > 0.01) {
        // Daytime: blue-ish aerial perspective
        float cosTheta = dot(viewDir, sunDirection);
        float phaseR = phaseRayleigh(cosTheta);
        float phaseM = phaseMie(cosTheta, 0.76);
        
        // Transmittance to sun at ground level
        vec3 sunTransmittance = getTransmittanceToSun(viewAltitude, sunHeight);
        
        // Inscattered light approximation
        inscatteredLight = (BETA_RAYLEIGH * phaseR + BETA_MIE * phaseM) * sunTransmittance * 500000.0;
        inscatteredLight *= dirLight.color.rgb * dayFactor;
    }
    
    // Twilight fog colors
    if (twilightPhase >= 1 && twilightPhase <= 3) {
        vec3 twilightColor;
        if (twilightPhase == 1) {
            // Civil twilight: warm orange/gold
            twilightColor = vec3(0.6, 0.35, 0.15);
        } else if (twilightPhase == 2) {
            // Nautical twilight: purple/blue
            twilightColor = vec3(0.2, 0.15, 0.3);
        } else {
            // Astronomical twilight: deep blue
            twilightColor = vec3(0.05, 0.05, 0.12);
        }
        inscatteredLight = mix(inscatteredLight, twilightColor, (1.0 - dayFactor) * twilightFactor);
    }
    
    // Night fog
    if (nightFactor > 0.01) {
        vec3 nightFogColor = vec3(0.008, 0.012, 0.025);
        inscatteredLight = mix(inscatteredLight, nightFogColor, nightFactor);
    }
    
    // Blend with FogColor for artistic control
    vec3 finalFogColor = mix(inscatteredLight, FogColor.rgb, 0.3);
    
    // Apply fog
    vec3 resultColor = mix(result, finalFogColor, fogFactor);

    // === TONE MAPPING ===
    vec3 mapped = resultColor * Exposure;
    
    // Selectable tone mapping
    if (toneMapMode == 0) {
        // ACES Filmic (default, UE5-style)
        mapped = ToneMap_ACES(mapped);
    } else if (toneMapMode == 1) {
        // Filmic (cinematic)
        mapped = ToneMap_Filmic(mapped);
    } else {
        // Reinhard (simple)
        mapped = ToneMap_Reinhard(mapped);
    }

    // Gamma Correction
    mapped = pow(mapped, vec3(1.0 / Gamma));

    FragColor = vec4(mapped, 1.0);
}
