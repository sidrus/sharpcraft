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

uniform float exposure;
uniform float gamma;

struct DirLight {
    vec3 direction;
    vec3 color;
};

struct PointLight {
    vec3 position;
    vec3 color;
    float intensity;
    float constant;
    float linear;
    float quadratic;
};

uniform DirLight dirLight;
#define NR_POINT_LIGHTS 4
uniform PointLight pointLights[NR_POINT_LIGHTS];

uniform vec3 viewPos;
uniform vec3 fogColor = vec3(0.53, 0.81, 0.92);
uniform float fogNear = 30.0;
uniform float fogFar = 100.0;

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
        ao = mix(1.0, mapValue, aoMapStrength);
    }

    float specMask = 1.0;
    if (useSpecular) {
        specMask = texture(specularMap, TexCoord).r;
    }

    vec3 baseColor = texColor.rgb;
    vec3 viewDir = normalize(viewPos - FragPos);

    // 3. Lighting Accumulation
    // Start with a small global ambient contribution
    vec3 result = vec3(0.1) * baseColor * ao;

    // Add Directional Light (Sun)
    result += CalcDirLight(dirLight, norm, viewDir, baseColor, specMask, ao);

    // Add Point Lights (Torches/Lamps)
    for(int i = 0; i < NR_POINT_LIGHTS; i++) {
        result += CalcPointLight(pointLights[i], norm, FragPos, viewDir, baseColor, specMask, ao);
    }

    // 4. Fog and Final Output
    float fogFactor = clamp((FragDistance - fogNear) / (fogFar - fogNear), 0.0, 1.0);
    vec3 finalColor = mix(result, fogColor, fogFactor);

    // HDR Tone Mapping (Exposure)
    vec3 mapped = vec3(1.0) - exp(-finalColor * exposure);

    // Gamma Correction
    mapped = pow(mapped, vec3(1.0 / gamma));

    FragColor = vec4(mapped, 1.0);
}

vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir, vec3 baseColor, float specMask, float ao) {
    vec3 lightDir = normalize(-light.direction);
    // Diffuse
    float diff = max(dot(normal, lightDir), 0.0);
    // Specular
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);

    vec3 diffuse = light.color * diff * baseColor;
    vec3 specular = light.color * (spec * specMask * specularMapStrength);

    return (diffuse + specular) * ao;
}

vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir, vec3 baseColor, float specMask, float ao) {
    vec3 lightDir = normalize(light.position - fragPos);
    // Diffuse
    float diff = max(dot(normal, lightDir), 0.0);
    // Specular
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);

    // Attenuation
    float distance = length(light.position - fragPos);
    // Standard attenuation formula using quadratic drop-off
    // Constant is usually 1.0, Linear 0.09, Quadratic 0.032 for a decent range
    float attenuation = 1.0 / (1.0 + 0.09 * distance + 0.032 * (distance * distance));

    vec3 diffuse = light.color * diff * baseColor;
    vec3 specular = light.color * (spec * specMask * specularMapStrength);

    return (diffuse + specular) * attenuation * ao * light.intensity;
}