#version 450 core
in vec3 Normal;
in vec3 FragPos;
in float FragDistance;
in vec2 TexCoord;
in mat3 TBN;

out vec4 FragColor;

uniform sampler2D textureAtlas;
uniform sampler2D normalMap;
uniform bool useNormalMap;
uniform float normalStrength;

uniform sampler2D aoMap;
uniform bool useAO;
uniform float aoMapStrength;

uniform sampler2D metallicMap;
uniform bool useMetallic;
uniform float metallicStrength;

uniform sampler2D roughnessMap;
uniform bool useRoughness;
uniform float roughnessStrength;

uniform samplerCube irradianceMap;
uniform samplerCube prefilterMap;
uniform sampler2D brdfLUT;
uniform bool useIBL;

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
    DirLight dirLight;
    PointLight pointLights[4];
};

#include "../Common/math.glsl"
#include "../Common/BRDF.glsl"
#include "../Common/lighting.glsl"

vec3 ACES(vec3 x) {
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

void main() {
    vec4 texColor = texture(textureAtlas, TexCoord);
    if(texColor.a < 0.1)
        discard;

    // 1. Setup Material Properties
    vec3 albedo = texColor.rgb;
    
    vec3 norm;
    if (useNormalMap) {
        norm = texture(normalMap, TexCoord).rgb * 2.0 - 1.0;
        norm.xy *= normalStrength;
        norm = normalize(TBN * normalize(norm));
    } else {
        norm = normalize(Normal);
    }

    float metallic = 0.0;
    if (useMetallic) {
        metallic = texture(metallicMap, TexCoord).r * metallicStrength;
    }
    
    float roughness = 0.5;
    if (useRoughness) {
        roughness = texture(roughnessMap, TexCoord).r * roughnessStrength;
    }
    roughness = clamp(roughness, 0.05, 1.0);

    float ao = 1.0;
    if (useAO) {
        float mapValue = texture(aoMap, TexCoord).r;
        ao = clamp(mix(1.0, mapValue, aoMapStrength), 0.0, 1.0);
    }

    vec3 V = normalize(ViewPos.xyz - FragPos);
    
    vec3 F0 = vec3(0.04); 
    F0 = mix(F0, albedo, metallic);

    // 2. Lighting Accumulation
    vec3 Lo = vec3(0.0);

    // Add Directional Light
    if (length(dirLight.color.xyz) > 0.0) {
        vec3 L = normalize(-dirLight.direction.xyz);
        Lo += CalcPBRLighting(L, V, norm, F0, albedo, metallic, roughness, dirLight.color.xyz);
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

    vec3 ambient;
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
        ambient = vec3(0.03) * albedo * ao;
    }
    
    vec3 result = ambient + Lo;

    // 3. Fog and Final Output
    float fogDistance = FogFar - FogNear;
    float fogFactor = clamp((FragDistance - FogNear) / max(fogDistance, 0.0001), 0.0, 1.0);
    vec3 finalColor = mix(result, FogColor.xyz, fogFactor);

    // Exposure
    finalColor *= Exposure;

    // ACES Tone Mapping
    vec3 mapped = ACES(finalColor);

    // Gamma Correction
    mapped = pow(mapped, vec3(1.0 / Gamma));

    FragColor = vec4(mapped, 1.0);
}