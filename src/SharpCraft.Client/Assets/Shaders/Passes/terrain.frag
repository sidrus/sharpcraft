#version 450 core
out vec4 FragColor;

in vec3 Normal;
in vec3 FragPos;
in vec2 TexCoord;
in mat3 TBN;

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

uniform sampler2DArrayShadow shadowMap;

// Image-based lighting (research §4.2).
uniform bool useIBL;
uniform samplerCube irradianceMap;
uniform samplerCube prefilterMap;
uniform sampler2D brdfLUT;

layout (std140, binding = 0) uniform SceneData {
    mat4 ViewProjection;
    vec4 ViewPos;
    vec4 FogColor;
    float FogNear;
    float FogFar;
    float Exposure;
    float Gamma;
    mat4 View;
};

struct DirLight {
    vec4 direction;
    vec4 color;
};

layout (std140, binding = 1) uniform LightingData {
    mat4 LightSpaceMatrix;
    DirLight dirLight;
};

// Cascaded shadow maps (research §8).
layout (std140, binding = 2) uniform CsmData {
    mat4 lightSpaceMatrices[4];
    vec4 cascadeSplits; // x..w = far view-distance of each cascade
    vec4 csmParams;     // x = cascadeCount, y = shadowMapSize
};

#include "../Common/math.glsl"
#include "../Common/BRDF.glsl"
#include "../Common/lighting.glsl"
#include "../Common/shadows.glsl"
#include "../Common/clusters.glsl"

// Clustered forward+ light buffers (research §2), filled by the compute cull pass. SSBO bindings
// are a separate namespace from the UBO bindings above.
layout(std430, binding = 1) readonly buffer Lights          { Light lights[]; };
layout(std430, binding = 2) readonly buffer LightGrid       { uvec2 lightGrid[]; };
layout(std430, binding = 3) readonly buffer GlobalIndexList { uint globalLightIndex[]; };

uniform vec3 clusterGridSize;
uniform vec2 clusterScreenSize;
uniform float clusterZNear;
uniform float clusterZFar;

// Screen-space ambient occlusion (research §7), multiplied into ambient only.
uniform bool useGtao;
uniform sampler2D gtaoTexture;
uniform vec2 invScreenSize;

// Contact shadows (research §7/§8): short screen-space ray toward the sun.
uniform bool useContactShadows;
uniform sampler2D sceneDepthTex;
uniform mat4 contactInvViewProj;

// March a short ray toward the sun against the opaque depth. Returns 0 if an occluder is hit
// (in contact shadow), 1 otherwise — fills the small contact gaps CSM can't resolve.
float contactShadow(vec3 worldPos, vec3 L) {
    const int STEPS = 16;
    const float MAX_DIST = 0.6;
    const float THICKNESS = 0.4;
    float stepLen = MAX_DIST / float(STEPS);
    vec3 rayPos = worldPos + L * 0.03;

    for (int i = 0; i < STEPS; i++) {
        rayPos += L * stepLen;
        vec4 clip = ViewProjection * vec4(rayPos, 1.0);
        if (clip.w <= 0.0) break;
        vec2 uv = (clip.xy / clip.w) * 0.5 + 0.5;
        if (any(lessThan(uv, vec2(0.0))) || any(greaterThan(uv, vec2(1.0)))) break;

        float sd = texture(sceneDepthTex, uv).r;
        if (sd <= 0.0) continue;
        vec4 swH = contactInvViewProj * vec4(uv * 2.0 - 1.0, sd, 1.0);
        vec3 sw = swH.xyz / swH.w;

        float rayDist = distance(rayPos, ViewPos.xyz);
        float sceneDist = distance(sw, ViewPos.xyz);
        if (rayDist > sceneDist + 0.01 && rayDist - sceneDist < THICKNESS) {
            return 0.0;
        }
    }
    return 1.0;
}

void main() {
    vec4 texColor = texture(textureAtlas, TexCoord);
    if (texColor.a < 0.1) discard;
    vec3 albedo = texColor.rgb;

    vec3 norm;
    if (useNormalMap) {
        vec3 baseNormal = texture(normalMap, TexCoord).rgb * 2.0 - 1.0;
        baseNormal.xy *= normalStrength;
        norm = normalize(TBN * baseNormal);
    } else {
        norm = normalize(Normal);
    }

    float metallic  = useMetallic  ? texture(metallicMap, TexCoord).r * metallicStrength : 0.0;
    float roughness = useRoughness ? texture(roughnessMap, TexCoord).r * roughnessStrength : 0.7;
    roughness = clamp(roughness, 0.05, 1.0);
    float ao = useAO ? clamp(mix(1.0, texture(aoMap, TexCoord).r, aoMapStrength), 0.0, 1.0) : 1.0;

    vec3 V = normalize(ViewPos.xyz - FragPos);
    roughness = specularAntiAliasing(norm, roughness);
    vec3 F0 = mix(vec3(0.04), albedo, metallic);

    // Direct sun light with cascaded soft shadows. Cascade is picked by camera distance
    // (radial distance is a safe, conservative proxy for view-space depth).
    vec3 Lo = vec3(0.0);
    if (length(dirLight.color.xyz) > 0.0) {
        vec3 L = normalize(-dirLight.direction.xyz);

        int cascadeCount = int(csmParams.x);
        float viewDist = length(FragPos - ViewPos.xyz);
        int layer = cascadeCount - 1;
        for (int i = 0; i < cascadeCount; i++) {
            if (viewDist < cascadeSplits[i]) { layer = i; break; }
        }

        // Normal-offset bias (research §8): nudge the receiver sample toward the occluder along
        // its surface normal, scaled by the cascade's world texel size (~ proportional to its far
        // split). Unlike depth bias this does NOT explode into horizontal detachment (peter-
        // panning) at grazing sun angles — it scales with texel size, not sun elevation.
        float normalOffset = cascadeSplits[layer] * 0.0016;
        vec3 shadowPos = FragPos + norm * normalOffset;
        vec4 fragPosLightSpace = lightSpaceMatrices[layer] * vec4(shadowPos, 1.0);
        float shadow = CalcShadowCSM(shadowMap, fragPosLightSpace, layer, norm, L, 0.01);

        // Contact shadow refines the cascade result at small-scale contact points.
        float contact = useContactShadows ? contactShadow(FragPos, L) : 1.0;
        Lo += (1.0 - shadow) * contact * CalcPBRLighting(L, V, norm, F0, albedo, metallic, roughness, dirLight.color.xyz);
    }

    // Punctual lights via clustered forward+ (research §2): look up this fragment's cluster and
    // shade only the lights the compute cull pass assigned to it.
    {
        float viewZ = (View * vec4(FragPos, 1.0)).z;
        uint cluster = clusterIndex(gl_FragCoord.xy, viewZ, clusterScreenSize,
                                    uvec3(clusterGridSize), clusterZNear, clusterZFar);
        uint offset = lightGrid[cluster].x;
        uint count = lightGrid[cluster].y;
        for (uint li = 0u; li < count; li++) {
            Light light = lights[globalLightIndex[offset + li]];
            vec3 d = light.positionRange.xyz - FragPos;
            float dist = length(d);
            if (dist > light.positionRange.w) continue; // outside cull radius
            float att = 1.0 / (light.atten.x + light.atten.y * dist + light.atten.z * dist * dist);
            vec3 radiance = light.color.rgb * light.color.w * att;
            Lo += CalcPBRLighting(normalize(d), V, norm, F0, albedo, metallic, roughness, radiance);
        }
    }

    // Ambient: image-based lighting when available, else a simple sky-driven fill.
    vec3 nightAmbient = vec3(0.015, 0.02, 0.035) * albedo * ao;
    vec3 ambient;
    if (useIBL) {
        float NoV = max(dot(norm, V), 1e-4);
        vec3 F = fresnelSchlickRoughness(NoV, F0, roughness);
        vec3 kD = (1.0 - F) * (1.0 - metallic);

        // Diffuse irradiance.
        vec3 diffuse = texture(irradianceMap, norm).rgb * albedo;

        // Specular split-sum: prefiltered env * (F * scale + bias).
        const float MAX_REFLECTION_LOD = 4.0; // PrefilterMips - 1
        vec3 R = reflect(-V, norm);
        vec3 prefiltered = textureLod(prefilterMap, R, roughness * MAX_REFLECTION_LOD).rgb;
        vec2 dfg = texture(brdfLUT, vec2(NoV, roughness)).rg;
        vec3 specular = prefiltered * (F * dfg.x + dfg.y);

        // Multi-scatter energy compensation (§3.3) — keeps rough metals from going too dark.
        specular *= 1.0 + F0 * (1.0 / max(dfg.y, 1e-3) - 1.0);

        // Specular occlusion (§4.3) + horizon occlusion (§4.2): fade reflections that point
        // into the geometric surface (strong normal maps otherwise leak light from behind).
        float specOcc = computeSpecularAO(NoV, ao, roughness);
        float horizon = clamp(1.0 + dot(R, normalize(Normal)), 0.0, 1.0);
        horizon *= horizon;
        specular *= specOcc * horizon;

        ambient = max((kD * diffuse + specular) * ao, nightAmbient);
    } else {
        vec3 skyDir = normalize(-dirLight.direction.xyz);
        float skyFactor = smoothstep(-0.15, 0.4, skyDir.y);
        vec3 twilight = mix(vec3(1.0, 0.5, 0.2), vec3(0.4, 0.6, 1.0), smoothstep(0.0, 0.3, skyDir.y));
        ambient = max(twilight * skyFactor * 0.15 * albedo * ao, nightAmbient);
    }

    // Screen-space AO darkens ambient only (research §7), never the direct light.
    if (useGtao) {
        float gtao = texture(gtaoTexture, gl_FragCoord.xy * invScreenSize).r;
        ambient *= gtao;
    }

    vec3 result = ambient + Lo;
    FragColor = vec4(result, 1.0); // true HDR radiance; exposure + tonemap in fxaa.frag (§5.2)
}
