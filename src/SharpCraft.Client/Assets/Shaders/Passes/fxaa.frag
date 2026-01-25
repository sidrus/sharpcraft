#version 450 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D screenTexture;
uniform sampler2D bloomTexture;
uniform sampler2D volumetricTexture;
uniform float bloomIntensity;
uniform float volumetricIntensity;
uniform vec2 inverseScreenSize;
uniform float time;
uniform bool isUnderwater;

// Post-processing uniforms
uniform float vignetteIntensity = 0.0;
uniform float chromaticAberration = 0.0;
uniform int toneMapMode = 0; // 0=ACES, 1=Filmic, 2=Reinhard

// FXAA parameters
#define FXAA_SPAN_MAX 8.0
#define FXAA_REDUCE_MUL (1.0/8.0)
#define FXAA_REDUCE_MIN (1.0/128.0)

// ============================================================================
// Tone Mapping Functions
// ============================================================================

// ACES Filmic Tone Mapping (Narkowicz approximation)
vec3 toneMapACES(vec3 x) {
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

// Filmic tone mapping (Uncharted 2 style)
vec3 toneMapFilmic(vec3 x) {
    vec3 X = max(vec3(0.0), x - 0.004);
    return (X * (6.2 * X + 0.5)) / (X * (6.2 * X + 1.7) + 0.06);
}

// Reinhard tone mapping
vec3 toneMapReinhard(vec3 x) {
    return x / (x + vec3(1.0));
}

vec3 applyToneMapping(vec3 color) {
    if (toneMapMode == 0) {
        return toneMapACES(color);
    } else if (toneMapMode == 1) {
        return toneMapFilmic(color);
    } else {
        return toneMapReinhard(color);
    }
}

// ============================================================================
// Post-Processing Effects
// ============================================================================

vec3 applyVignette(vec3 color, vec2 uv) {
    if (vignetteIntensity <= 0.0) return color;
    
    vec2 center = uv - 0.5;
    float dist = length(center);
    float vignette = 1.0 - smoothstep(0.3, 0.9, dist * vignetteIntensity * 2.0);
    return color * vignette;
}

vec3 applyChromaticAberration(sampler2D tex, vec2 uv) {
    if (chromaticAberration <= 0.0) return texture(tex, uv).rgb;
    
    vec2 center = uv - 0.5;
    float dist = length(center);
    vec2 dir = normalize(center) * chromaticAberration * dist;
    
    float r = texture(tex, uv + dir).r;
    float g = texture(tex, uv).g;
    float b = texture(tex, uv - dir).b;
    
    return vec3(r, g, b);
}

vec3 applyFXAA(vec2 texCoords, sampler2D tex) {
    vec3 rgbNW = textureLod(tex, texCoords + (vec2(-1.0, -1.0) * inverseScreenSize), 0.0).xyz;
    vec3 rgbNE = textureLod(tex, texCoords + (vec2(1.0, -1.0) * inverseScreenSize), 0.0).xyz;
    vec3 rgbSW = textureLod(tex, texCoords + (vec2(-1.0, 1.0) * inverseScreenSize), 0.0).xyz;
    vec3 rgbSE = textureLod(tex, texCoords + (vec2(1.0, 1.0) * inverseScreenSize), 0.0).xyz;
    vec3 rgbM  = textureLod(tex, texCoords, 0.0).xyz;

    vec3 luma = vec3(0.299, 0.587, 0.114);
    float lumaNW = dot(rgbNW, luma);
    float lumaNE = dot(rgbNE, luma);
    float lumaSW = dot(rgbSW, luma);
    float lumaSE = dot(rgbSE, luma);
    float lumaM  = dot(rgbM,  luma);

    float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
    float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));

    vec2 dir;
    dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
    dir.y =  ((lumaNW + lumaSW) - (lumaNE + lumaSE));

    float dirReduce = max(
        (lumaNW + lumaNE + lumaSW + lumaSE) * (0.25 * FXAA_REDUCE_MUL),
        FXAA_REDUCE_MIN);

    float rcpDirMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + dirReduce);

    dir = min(vec2( FXAA_SPAN_MAX,  FXAA_SPAN_MAX),
          max(vec2(-FXAA_SPAN_MAX, -FXAA_SPAN_MAX),
          dir * rcpDirMin)) * inverseScreenSize;

    vec3 rgbA = (1.0/2.0) * (
        textureLod(tex, texCoords.xy + dir * (1.0/3.0 - 0.5), 0.0).xyz +
        textureLod(tex, texCoords.xy + dir * (2.0/3.0 - 0.5), 0.0).xyz);
    vec3 rgbB = rgbA * (1.0/2.0) + (1.0/4.0) * (
        textureLod(tex, texCoords.xy + dir * (0.0/3.0 - 0.5), 0.0).xyz +
        textureLod(tex, texCoords.xy + dir * (3.0/3.0 - 0.5), 0.0).xyz);
    float lumaB = dot(rgbB, luma);

    if((lumaB < lumaMin) || (lumaB > lumaMax)) {
        return rgbA;
    } else {
        return rgbB;
    }
}

void main()
{
    vec2 distortedTexCoords = TexCoords;
    if (isUnderwater) {
        distortedTexCoords.x += sin(distortedTexCoords.y * 10.0 + time * 2) * 0.001;
        distortedTexCoords.y += cos(distortedTexCoords.x * 10.0 + time * 2) * 0.001;
    }

    // Apply chromatic aberration before FXAA if enabled
    vec3 color;
    if (chromaticAberration > 0.0) {
        color = applyChromaticAberration(screenTexture, distortedTexCoords);
    } else {
        color = applyFXAA(distortedTexCoords, screenTexture);
    }
    
    vec3 bloom = texture(bloomTexture, distortedTexCoords).rgb;
    vec4 volumetricData = texture(volumetricTexture, distortedTexCoords);
    vec3 volumetric = volumetricData.rgb;
    float volumetricFog = volumetricData.a;
    
    // Add bloom
    color += bloom * bloomIntensity;
    
    // Add volumetric rays
    color += volumetric * volumetricIntensity;
    
    // Underwater effect
    if (isUnderwater) {
        vec3 underwaterColor = vec3(0.0, 0.4, 0.8);
        color = mix(color, underwaterColor, 0.4);
    }
    
    // Apply tone mapping (HDR to LDR)
    color = applyToneMapping(color);
    
    // Apply vignette (after tone mapping, in LDR space)
    color = applyVignette(color, TexCoords);

    FragColor = vec4(color, 1.0);
}
