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

const float PI = 3.14159265359;

// ----------------------------------------------------------------------------
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
// ----------------------------------------------------------------------------
float GeometrySchlickGGX(float NdotV, float roughness) {
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float num = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return num / max(denom, 0.0000001);
}
// ----------------------------------------------------------------------------
float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness) {
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}
// ----------------------------------------------------------------------------
vec3 fresnelSchlick(float cosTheta, vec3 F0) {
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}
// ----------------------------------------------------------------------------
vec3 CalcPBRLighting(vec3 L, vec3 V, vec3 N, vec3 F0, vec3 albedo, float metallic, float roughness, vec3 lightColor) {
    vec3 H = normalize(V + L);
    
    // Cook-Torrance BRDF
    float NDF = DistributionGGX(N, H, roughness);
    float G   = GeometrySmith(N, V, L, roughness);
    vec3 F    = fresnelSchlick(max(dot(H, V), 0.0), F0);
    
    vec3 numerator    = NDF * G * F;
    float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001;
    vec3 specular = numerator / denominator;
    
    vec3 kS = F;
    vec3 kD = vec3(1.0) - kS;
    kD *= 1.0 - metallic;
    
    float NdotL = max(dot(N, L), 0.0);
    return (kD * albedo / PI + specular) * lightColor * NdotL;
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

    vec3 ambient = vec3(0.03) * albedo * ao;
    vec3 result = ambient + Lo;

    // 3. Fog and Final Output
    float fogDistance = FogFar - FogNear;
    float fogFactor = clamp((FragDistance - FogNear) / max(fogDistance, 0.0001), 0.0, 1.0);
    vec3 finalColor = mix(result, FogColor.xyz, fogFactor);

    // HDR Tone Mapping (Exposure)
    vec3 mapped = vec3(1.0) - exp(-finalColor * Exposure);

    // Gamma Correction
    mapped = pow(mapped, vec3(1.0 / Gamma));

    FragColor = vec4(mapped, 1.0);
}