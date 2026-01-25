#version 450 core

in vec3 Normal;
in vec3 FragPos;
in float FragDistance;
in vec2 TexCoord;
in mat3 TBN;

// G-Buffer outputs (Multiple Render Targets)
layout(location = 0) out vec4 gAlbedoAO;     // RGB: Albedo, A: AO
layout(location = 1) out vec4 gNormal;        // RGB: World Normal (encoded), A: unused
layout(location = 2) out vec4 gMaterial;      // R: Metallic, G: Roughness, B: unused, A: unused
layout(location = 3) out vec4 gPosition;      // RGB: World Position, A: Fragment Distance

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

void main() {
    vec4 texColor = texture(textureAtlas, TexCoord);
    if(texColor.a < 0.1)
        discard;

    // Albedo
    vec3 albedo = texColor.rgb;
    
    // Normal
    vec3 norm;
    if (useNormalMap) {
        norm = texture(normalMap, TexCoord).rgb * 2.0 - 1.0;
        norm.xy *= normalStrength;
        norm = normalize(TBN * normalize(norm));
    } else {
        norm = normalize(Normal);
    }

    // Metallic
    float metallic = 0.0;
    if (useMetallic) {
        metallic = texture(metallicMap, TexCoord).r * metallicStrength;
    }
    
    // Roughness
    float roughness = 0.5;
    if (useRoughness) {
        roughness = texture(roughnessMap, TexCoord).r * roughnessStrength;
    }
    roughness = clamp(roughness, 0.05, 1.0);

    // Ambient Occlusion
    float ao = 1.0;
    if (useAO) {
        float mapValue = texture(aoMap, TexCoord).r;
        ao = clamp(mix(1.0, mapValue, aoMapStrength), 0.0, 1.0);
    }

    // Output to G-Buffer
    gAlbedoAO = vec4(albedo, ao);
    gNormal = vec4(norm * 0.5 + 0.5, 1.0);  // Encode normal to [0,1] range
    gMaterial = vec4(metallic, roughness, 0.0, 1.0);
    gPosition = vec4(FragPos, FragDistance);
}
