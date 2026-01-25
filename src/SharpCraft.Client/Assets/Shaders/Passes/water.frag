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
uniform float time;

uniform sampler2DShadow shadowMap;

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
    mat4 LightSpaceMatrix;
    DirLight dirLight;
    PointLight pointLights[4];
};

#include "../Common/math.glsl"
#include "../Common/BRDF.glsl"
#include "../Common/shadows.glsl"

// Water properties - realistic clear lake water
const float WATER_IOR = 1.333;
const float WATER_ROUGHNESS = 0.05;

// Realistic water colors (clear lake)
const vec3 WATER_ABSORPTION = vec3(0.45, 0.09, 0.06); // How much light is absorbed per meter
const vec3 WATER_SCATTER = vec3(0.02, 0.03, 0.04);    // Subsurface scatter color

// Fresnel for water (Schlick approximation)
float fresnelWater(float cosTheta) {
    float f0 = pow((1.0 - WATER_IOR) / (1.0 + WATER_IOR), 2.0);
    return f0 + (1.0 - f0) * pow(1.0 - cosTheta, 5.0);
}

// Animated water normal from waves
vec3 getWaterNormal(vec2 worldPos, float t) {
    // Multiple wave frequencies for realistic water
    float speed1 = t * 0.3;
    float speed2 = t * 0.2;
    float speed3 = t * 0.15;
    
    vec2 uv1 = worldPos * 0.5 + vec2(speed1, speed1 * 0.7);
    vec2 uv2 = worldPos * 0.3 + vec2(-speed2 * 0.8, speed2);
    vec2 uv3 = worldPos * 0.15 + vec2(speed3, -speed3 * 0.5);
    
    vec3 n1, n2, n3;
    
    if (useNormalMap) {
        n1 = texture(normalMap, uv1).rgb * 2.0 - 1.0;
        n2 = texture(normalMap, uv2).rgb * 2.0 - 1.0;
        n3 = texture(normalMap, uv3).rgb * 2.0 - 1.0;
    } else {
        // Procedural waves
        float wave1 = sin(uv1.x * 8.0 + uv1.y * 6.0);
        float wave2 = sin(uv2.x * 12.0 - uv2.y * 10.0);
        float wave3 = sin(uv3.x * 4.0 + uv3.y * 3.0);
        
        n1 = vec3(cos(wave1) * 0.08, cos(wave1) * 0.08, 1.0);
        n2 = vec3(cos(wave2) * 0.04, cos(wave2) * 0.04, 1.0);
        n3 = vec3(cos(wave3) * 0.02, cos(wave3) * 0.02, 1.0);
    }
    
    // Blend normals
    vec3 normal = normalize(n1 * 0.5 + n2 * 0.3 + n3 * 0.2);
    normal.xy *= normalStrength * 0.3; // Subtle waves
    
    return normalize(normal);
}

void main() {
    vec3 V = normalize(ViewPos.xyz - FragPos);
    
    // Get animated water normal
    vec3 localNormal = getWaterNormal(FragPos.xz * 0.1, time);
    vec3 N = normalize(TBN * localNormal);
    
    // View-dependent factors
    float NoV = max(dot(N, V), 0.0);
    float fresnel = fresnelWater(NoV);
    
    // === LIGHTING ===
    vec3 L = normalize(-dirLight.direction.xyz);
    float sunHeight = max(L.y, 0.0);
    
    // Shadow calculation
    vec4 fragPosLightSpace = LightSpaceMatrix * vec4(FragPos, 1.0);
    float shadow = 0.0;
    if (length(dirLight.color.xyz) > 0.0) {
        shadow = CalcShadow(shadowMap, fragPosLightSpace, N, L);
    }
    
    // Effective light (reduced by shadow)
    float lightFactor = (1.0 - shadow) * sunHeight;
    vec3 lightColor = dirLight.color.xyz * lightFactor;
    
    // Moonlight
    vec3 moonDir = normalize(dirLight.direction.xyz);
    float sunVisible = clamp(L.y * 5.0, 0.0, 1.0);
    float moonIntensity = (1.0 - sunVisible) * 0.08;
    vec3 moonLightColor = vec3(0.4, 0.5, 0.7) * moonIntensity;
    
    // === WATER COLOR ===
    // Base water color - very subtle tint, mostly transparent
    vec3 waterTint = vec3(0.15, 0.25, 0.3); // Subtle blue-green tint
    
    // Depth simulation based on view angle (steeper = looks deeper)
    float depthFactor = pow(1.0 - NoV, 3.0);
    vec3 deepColor = vec3(0.02, 0.08, 0.12); // Dark blue-green for "deep" water
    vec3 baseWaterColor = mix(waterTint, deepColor, depthFactor * 0.5);
    
    // === SPECULAR REFLECTION ===
    vec3 H = normalize(V + L);
    float NoH = max(dot(N, H), 0.0);
    float NoL = max(dot(N, L), 0.0);
    float HoV = max(dot(H, V), 0.0);
    
    // GGX specular for sun
    float D = DistributionGGX(N, H, WATER_ROUGHNESS);
    float G = GeometrySmith(N, V, L, WATER_ROUGHNESS);
    vec3 F = vec3(fresnelWater(HoV));
    
    vec3 specular = (D * G * F) / max(4.0 * NoV * NoL, 0.001);
    vec3 sunSpec = specular * lightColor * NoL;
    
    // Moon specular (very subtle)
    if (moonIntensity > 0.0) {
        vec3 Hm = normalize(V + moonDir);
        float NoHm = max(dot(N, Hm), 0.0);
        float Dm = DistributionGGX(N, Hm, WATER_ROUGHNESS);
        sunSpec += vec3(Dm * 0.1) * moonLightColor;
    }
    
    // Point light specular
    for(int i = 0; i < 4; i++) {
        if (pointLights[i].intensity > 0.0) {
            vec3 Lp = normalize(pointLights[i].position.xyz - FragPos);
            float dist = length(pointLights[i].position.xyz - FragPos);
            float attenuation = 1.0 / (pointLights[i].constant + 
                                       pointLights[i].linear * dist + 
                                       pointLights[i].quadratic * dist * dist);
            vec3 radiance = pointLights[i].color.xyz * pointLights[i].intensity * attenuation;
            
            vec3 Hp = normalize(V + Lp);
            float NoHp = max(dot(N, Hp), 0.0);
            float Dp = DistributionGGX(N, Hp, WATER_ROUGHNESS);
            sunSpec += vec3(Dp * 0.5) * radiance;
        }
    }
    
    // === SKY REFLECTION ===
    vec3 R = reflect(-V, N);
    vec3 skyReflection;
    
    if (useIBL) {
        skyReflection = textureLod(prefilterMap, R, WATER_ROUGHNESS * 4.0).rgb;
    } else {
        // Approximate sky color based on reflection direction
        float skyGrad = max(R.y, 0.0);
        vec3 skyZenith = vec3(0.1, 0.2, 0.4) * (sunHeight + 0.1);
        vec3 skyHorizon = vec3(0.3, 0.35, 0.45) * (sunHeight + 0.1);
        skyReflection = mix(skyHorizon, skyZenith, skyGrad);
        
        // Add sun reflection in sky
        float sunRefl = pow(max(dot(R, L), 0.0), 64.0);
        skyReflection += dirLight.color.xyz * sunRefl * sunHeight;
    }
    
    // === COMBINE ===
    // Water is mostly transparent with some tint, plus reflections
    vec3 diffuse = baseWaterColor * (lightColor + moonLightColor + vec3(0.02)) * 0.3;
    vec3 reflection = skyReflection * fresnel;
    
    vec3 waterColor = diffuse + reflection + sunSpec;
    
    // === ATMOSPHERIC FOG ===
    float fogDistance = FogFar - FogNear;
    float fogFactor = clamp((FragDistance - FogNear) / max(fogDistance, 0.0001), 0.0, 1.0);
    fogFactor = pow(fogFactor, 1.5);
    
    // Fog color based on time of day
    vec3 fogColor = FogColor.rgb * (sunHeight * 0.8 + 0.2);
    waterColor = mix(waterColor, fogColor, fogFactor);
    
    // === TRANSPARENCY ===
    // Water is more transparent when looking straight down, more reflective at glancing angles
    float alpha = mix(0.4, 0.85, fresnel);
    
    // Increase opacity in shadow and at night
    alpha = mix(alpha, alpha * 1.3, shadow * 0.3);
    alpha = mix(alpha, 0.7, (1.0 - sunHeight) * 0.3);
    
    alpha = clamp(alpha, 0.3, 0.9);
    
    // === TONE MAPPING & OUTPUT ===
    vec3 mapped = waterColor * Exposure;
    mapped = mapped / (mapped + vec3(1.0));
    mapped = pow(mapped, vec3(1.0 / Gamma));
    
    FragColor = vec4(mapped, alpha);
}
