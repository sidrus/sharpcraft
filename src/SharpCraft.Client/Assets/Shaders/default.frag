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

uniform sampler2D specularMap;
uniform bool useSpecular;
uniform float specularMapStrength;

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

// Function prototypes
vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir, vec3 baseColor, float specMask, float ao);
vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir, vec3 baseColor, float specMask, float ao);

void main() {
    vec4 texColor = texture(textureAtlas, TexCoord);
    if(texColor.a < 0.1)
    discard;

    // 1. Calculate Normals
    vec3 norm;
    if (useNormalMap) {
        norm = texture(normalMap, TexCoord).rgb * 2.0 - 1.0;
        norm.xy *= normalStrength;
        norm = normalize(TBN * normalize(norm));
    } else {
        norm = normalize(Normal);
    }

    // 2. Sample AO and Specular Maps
    float ao = 1.0;
    if (useAO) {
        float mapValue = texture(aoMap, TexCoord).r;
        ao = clamp(mix(1.0, mapValue, aoMapStrength), 0.0, 1.0);
    }

    float specMask = 1.0;
    if (useSpecular) {
        specMask = texture(specularMap, TexCoord).r;
    }

    vec3 baseColor = texColor.rgb;
    vec3 viewDir = normalize(ViewPos.xyz - FragPos);

    // 3. Lighting Accumulation
    // Start with a small global ambient contribution
    vec3 result = vec3(0.1) * baseColor * ao;

    // Add Directional Light (Sun)
    if (length(dirLight.color.xyz) > 0.0) {
        result += CalcDirLight(dirLight, norm, viewDir, baseColor, specMask, ao);
    }

    // Add Point Lights (Torches/Lamps)
    for(int i = 0; i < 4; i++) {
        if (pointLights[i].intensity > 0.0) {
            result += CalcPointLight(pointLights[i], norm, FragPos, viewDir, baseColor, specMask, ao);
        }
    }

    // 4. Fog and Final Output
    float fogDistance = FogFar - FogNear;
    float fogFactor = clamp((FragDistance - FogNear) / max(fogDistance, 0.0001), 0.0, 1.0);
    vec3 finalColor = mix(result, FogColor.xyz, fogFactor);

    // HDR Tone Mapping (Exposure)
    vec3 mapped = vec3(1.0) - exp(-finalColor * Exposure);

    // Gamma Correction
    mapped = pow(mapped, vec3(1.0 / Gamma));

    FragColor = vec4(mapped, 1.0);
}

vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir, vec3 baseColor, float specMask, float ao) {
    vec3 lightDir = normalize(-light.direction.xyz);
    // Diffuse
    float diff = max(dot(normal, lightDir), 0.0);
    // Specular
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);

    vec3 diffuse = light.color.xyz * diff * baseColor;
    vec3 specular = light.color.xyz * (spec * specMask * specularMapStrength);

    return (diffuse + specular) * ao;
}

vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir, vec3 baseColor, float specMask, float ao) {
    vec3 lightDir = normalize(light.position.xyz - fragPos);
    // Diffuse
    float diff = max(dot(normal, lightDir), 0.0);
    // Specular
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);

    // Attenuation
    float distance = length(light.position.xyz - fragPos);
    // Standard attenuation formula using quadratic drop-off
    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));

    vec3 diffuse = light.color.xyz * diff * baseColor;
    vec3 specular = light.color.xyz * (spec * specMask * specularMapStrength);

    return (diffuse + specular) * attenuation * ao * light.intensity;
}