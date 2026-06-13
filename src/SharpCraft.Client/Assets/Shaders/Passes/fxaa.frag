#version 450 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D screenTexture;     // HDR linear scene (true radiance; exposure applied here)
uniform vec2 inverseScreenSize;
uniform float time;
uniform bool isUnderwater;

// Auto-exposure (research §5.2): the adapted exposure computed by the histogram compute passes,
// multiplied before tonemap. manualExposure is the HUD slider, now acting as exposure compensation.
layout(std430, binding = 5) readonly buffer ExposureBuffer { float autoExposure; };
uniform float manualExposure;

// HDR bloom (research §5.6): the dual-filter pyramid result, lerped into the scene before tonemap.
uniform bool useBloom;
uniform sampler2D bloomTexture;
uniform float bloomStrength;

// Post-processing uniforms
uniform float vignetteIntensity = 0.0;
uniform float chromaticAberration = 0.0;
uniform int toneMapMode = 0; // 0=ACES (Hill fit), 1=AgX, 2=Reinhard
uniform bool useFXAA = true; // off when TAA already resolved spatial aliasing (research §9)

// FXAA parameters
#define FXAA_SPAN_MAX 8.0
#define FXAA_REDUCE_MUL (1.0/8.0)
#define FXAA_REDUCE_MIN (1.0/128.0)

// ============================================================================
// Tone Mapping (HDR linear -> display-referred linear)
//
// Output transform order (research §5.3–§5.5, §12.3):
//   composite HDR -> tonemap -> FXAA in display space -> sRGB OETF -> TPDF dither.
// FXAA is a luma-edge filter and must run AFTER tonemap/encode, not on HDR.
// GL_FRAMEBUFFER_SRGB is OFF, so we apply the sRGB OETF ourselves exactly once.
// ============================================================================

// ACES filmic (Stephen Hill fit). Returns display-referred linear; encode to sRGB afterwards.
const mat3 ACESInputMat = mat3(
    0.59719, 0.07600, 0.02840,
    0.35458, 0.90834, 0.13383,
    0.04823, 0.01566, 0.83777
);
const mat3 ACESOutputMat = mat3(
     1.60475, -0.10208, -0.00327,
    -0.53108,  1.10813, -0.07276,
    -0.07367, -0.00605,  1.07602
);

vec3 rrtAndOdtFit(vec3 v) {
    vec3 a = v * (v + 0.0245786) - 0.000090537;
    vec3 b = v * (0.983729 * v + 0.4329510) + 0.238081;
    return a / b;
}

vec3 toneMapACESHill(vec3 color) {
    color = ACESInputMat * color;
    color = rrtAndOdtFit(color);
    color = ACESOutputMat * color;
    return clamp(color, 0.0, 1.0);
}

// AgX (Troy Sobotka's transform, Benjamin Wrensch's minimal fit).
vec3 agxDefaultContrastApprox(vec3 x) {
    vec3 x2 = x * x;
    vec3 x4 = x2 * x2;
    return + 15.5   * x4 * x2
           - 40.14  * x4 * x
           + 31.96  * x4
           - 6.868  * x2 * x
           + 0.4298 * x2
           + 0.1191 * x
           - 0.00232;
}

vec3 toneMapAgX(vec3 val) {
    const mat3 agxMat = mat3(
        0.842479062253094,  0.0423282422610123, 0.0423756549057051,
        0.0784335999999992, 0.878468636469772,  0.0784336,
        0.0792237451477643, 0.0791661274605434, 0.879142973793104);
    const mat3 agxMatInv = mat3(
         1.19687900512017,   -0.0528968517574562, -0.0529716355144438,
        -0.0980208811401368,  1.15190312990417,   -0.0980434501171241,
        -0.0990297440797205, -0.0989611768448433,  1.15107367264116);
    const float minEv = -12.47393;
    const float maxEv = 4.026069;

    // Encode: inset matrix, log2 range, sigmoid contrast approximation.
    val = agxMat * val;
    val = clamp(log2(max(val, 1e-10)), minEv, maxEv);
    val = (val - minEv) / (maxEv - minEv);
    val = agxDefaultContrastApprox(val);

    // Outset matrix, then linearize (pow 2.2). The shared sRGB OETF below re-encodes once.
    val = agxMatInv * val;
    val = pow(max(val, 0.0), vec3(2.2));
    return val;
}

vec3 toneMapReinhard(vec3 x) {
    return x / (x + vec3(1.0));
}

vec3 applyToneMapping(vec3 color) {
    if (toneMapMode == 1) {
        return toneMapAgX(color);
    } else if (toneMapMode == 2) {
        return toneMapReinhard(color);
    }
    return toneMapACESHill(color);
}

// Piecewise sRGB OETF (linear -> display). A true encode, not a single power curve.
vec3 linearToSrgb(vec3 c) {
    c = clamp(c, 0.0, 1.0);
    bvec3 cutoff = lessThanEqual(c, vec3(0.0031308));
    vec3 lo = c * 12.92;
    vec3 hi = 1.055 * pow(c, vec3(1.0 / 2.4)) - 0.055;
    return mix(hi, lo, vec3(cutoff));
}

// ============================================================================
// Composition / resolve
// ============================================================================

vec3 compositeSceneHDR(vec2 uv) {
    vec3 color = texture(screenTexture, uv).rgb;
    // Bloom is energy from the scene's bright sources — composited in HDR before exposure/tonemap
    // (research §5.6): lerp the scene toward the bloom pyramid by a small factor.
    if (useBloom) {
        vec3 bloom = texture(bloomTexture, uv).rgb;
        color = mix(color, bloom, bloomStrength);
    }
    // Single exposure multiply before tonemap (research §5.2/§5.4): auto-exposure × manual comp.
    color *= autoExposure * manualExposure;
    if (isUnderwater) {
        color = mix(color, vec3(0.0, 0.4, 0.8), 0.4);
    }
    return color;
}

// HDR linear -> display-encoded sRGB. FXAA samples this so it runs in display space.
vec3 resolveLDR(vec2 uv) {
    vec3 hdr = compositeSceneHDR(uv);
    vec3 mapped = applyToneMapping(hdr);
    return linearToSrgb(mapped);
}

// ============================================================================
// Post-processing effects (display space)
// ============================================================================

vec3 applyVignette(vec3 color, vec2 uv) {
    if (vignetteIntensity <= 0.0) return color;

    vec2 center = uv - 0.5;
    float dist = length(center);
    float vignette = 1.0 - smoothstep(0.3, 0.9, dist * vignetteIntensity * 2.0);
    return color * vignette;
}

vec3 applyChromaticAberration(vec2 uv) {
    vec2 center = uv - 0.5;
    float dist = length(center);
    vec2 dir = normalize(center + 1e-6) * chromaticAberration * dist;

    float r = resolveLDR(uv + dir).r;
    float g = resolveLDR(uv).g;
    float b = resolveLDR(uv - dir).b;
    return vec3(r, g, b);
}

// FXAA operating on the tonemapped, sRGB-encoded image.
vec3 applyFXAA(vec2 texCoords) {
    vec3 rgbNW = resolveLDR(texCoords + vec2(-1.0, -1.0) * inverseScreenSize);
    vec3 rgbNE = resolveLDR(texCoords + vec2( 1.0, -1.0) * inverseScreenSize);
    vec3 rgbSW = resolveLDR(texCoords + vec2(-1.0,  1.0) * inverseScreenSize);
    vec3 rgbSE = resolveLDR(texCoords + vec2( 1.0,  1.0) * inverseScreenSize);
    vec3 rgbM  = resolveLDR(texCoords);

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

    vec3 rgbA = 0.5 * (
        resolveLDR(texCoords + dir * (1.0 / 3.0 - 0.5)) +
        resolveLDR(texCoords + dir * (2.0 / 3.0 - 0.5)));
    vec3 rgbB = rgbA * 0.5 + 0.25 * (
        resolveLDR(texCoords + dir * (0.0 / 3.0 - 0.5)) +
        resolveLDR(texCoords + dir * (3.0 / 3.0 - 0.5)));
    float lumaB = dot(rgbB, luma);

    if ((lumaB < lumaMin) || (lumaB > lumaMax)) {
        return rgbA;
    }
    return rgbB;
}

// ============================================================================
// Dithering (research §5.5): TPDF ±1 LSB applied after the OETF, just before the
// 8-bit framebuffer quantizes. A blue-noise texture would be ideal; a hash-based
// triangular dither is used here to keep the pass self-contained. Eliminates the
// banding that 8-bit sRGB output otherwise shows on sky gradients / dark fog.
// ============================================================================

float hash12(vec2 p) {
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}

vec3 ditherTPDF(vec2 fragCoord) {
    // Difference of two uniform samples => triangular PDF in [-1, 1]. ~1.5 LSB amplitude — a touch
    // above the textbook ±1 to fully break the very smooth, low-contrast sky gradients (whose
    // banding auto-exposure can stretch) at the cost of imperceptible extra noise.
    vec3 r0 = vec3(hash12(fragCoord),               hash12(fragCoord + 17.0), hash12(fragCoord + 41.0));
    vec3 r1 = vec3(hash12(fragCoord + 113.0), hash12(fragCoord + 71.0), hash12(fragCoord + 29.0));
    return (r0 - r1) * (1.5 / 255.0);
}

void main()
{
    vec2 uv = TexCoords;
    if (isUnderwater) {
        uv.x += sin(uv.y * 10.0 + time * 2.0) * 0.001;
        uv.y += cos(uv.x * 10.0 + time * 2.0) * 0.001;
    }

    // Tonemap + encode, with edge AA (or chromatic aberration) in display space.
    vec3 color;
    if (chromaticAberration > 0.0) {
        color = applyChromaticAberration(uv);
    } else if (useFXAA) {
        color = applyFXAA(uv);
    } else {
        color = resolveLDR(uv); // TAA handled AA; just tonemap + encode the center sample
    }

    // Vignette in display space.
    color = applyVignette(color, TexCoords);

    // Dither last, immediately before 8-bit quantization.
    color += ditherTPDF(gl_FragCoord.xy);

    FragColor = vec4(color, 1.0);
}
