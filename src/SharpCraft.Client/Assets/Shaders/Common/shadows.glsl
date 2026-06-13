#ifndef SHADOWS_GLSL
#define SHADOWS_GLSL

#include "math.glsl"

vec2 poissonDisk[16] = vec2[](
   vec2( -0.94201624, -0.39906216 ),
   vec2( 0.94558609, -0.76890725 ),
   vec2( -0.094184101, -0.92938870 ),
   vec2( 0.34495938, 0.29387760 ),
   vec2( -0.91588581, 0.45771432 ),
   vec2( -0.81544232, -0.87912464 ),
   vec2( -0.38277543, 0.27676845 ),
   vec2( 0.97484398, 0.75648379 ),
   vec2( 0.44323325, -0.97511554 ),
   vec2( 0.53742981, -0.47373420 ),
   vec2( -0.26496911, -0.41893023 ),
   vec2( 0.79197514, 0.19090188 ),
   vec2( -0.24188840, 0.99706507 ),
   vec2( -0.81409955, 0.91437590 ),
   vec2( 0.19984126, 0.78641367 ),
   vec2( 0.14383161, -0.14100790 )
);

float random(vec3 seed, int i) {
    vec4 seed4 = vec4(seed, i);
    float dot_product = dot(seed4, vec4(12.9898, 78.233, 45.164, 94.673));
    return fract(sin(dot_product) * 43758.5453);
}

// ============================================================================
// Fibonacci-disk PCF with physically-motivated penumbra radius
// ============================================================================

// Fibonacci spiral: well-distributed, low-discrepancy disk samples
vec2 fibonacciDisk(int i, int n) {
    const float goldenAngle = 2.39996323; // 2π / φ²
    float theta = float(i) * goldenAngle;
    float r = sqrt(float(i + 1) / float(n));
    return vec2(cos(theta), sin(theta)) * r;
}

// 32-sample PCF with variable penumbra radius.
// lightSize controls the angular size of the light source (sun ~0.01).
float CalcShadowPCSS(sampler2DShadow shadowMap, vec4 fragPosLightSpace, vec3 normal, vec3 lightDir, float lightSize) {
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // Reversed-Z + glClipControl(ZeroToOne): the shadow map uses a conventional ortho whose
    // NDC z is already in [0,1], so only remap xy to UV; z stays as the comparison depth.
    projCoords.xy = projCoords.xy * 0.5 + 0.5;

    if (projCoords.z > 1.0) return 0.0;

    vec2 texelSize = 1.0 / vec2(textureSize(shadowMap, 0));

    // Slope-scaled bias: reduces acne on steep surfaces
    float cosTheta = clamp(dot(normal, lightDir), 0.0, 1.0);
    float bias = max(0.0025 * (1.0 - cosTheta), 0.0004);

    // Penumbra radius: proportional to shadow-map depth (receiver distance).
    // Simulates PCSS softening without a separate blocker-search pass.
    float receiverDepth = projCoords.z;
    float penumbraWidth = lightSize * receiverDepth * 80.0;
    penumbraWidth = clamp(penumbraWidth, 1.5, 10.0);

    // Per-pixel rotation to break up the regular disk pattern
    float rotAngle = random(floor(projCoords.xyz * 1000.0), 0) * TWO_PI;
    float sinA = sin(rotAngle), cosA = cos(rotAngle);

    float shadow = 0.0;
    const int SAMPLES = 32;

    for (int i = 0; i < SAMPLES; i++) {
        vec2 d = fibonacciDisk(i, SAMPLES);
        // Rotate sample by the per-pixel angle to avoid pattern repetition
        vec2 offset = vec2(d.x * cosA - d.y * sinA, d.x * sinA + d.y * cosA);
        shadow += texture(shadowMap, vec3(projCoords.xy + offset * texelSize * penumbraWidth, projCoords.z - bias));
    }

    return 1.0 - shadow / float(SAMPLES);
}

// ============================================================================
// Cascaded shadow maps (research §8): PCSS-style soft PCF against one cascade
// layer of a sampler2DArrayShadow. Cascade selection is done by the caller.
// ============================================================================
float CalcShadowCSM(sampler2DArrayShadow shadowMap, vec4 fragPosLightSpace, int layer,
                    vec3 normal, vec3 lightDir, float lightSize) {
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // Conventional ortho per cascade → NDC z already in [0,1]; only remap xy to UV.
    projCoords.xy = projCoords.xy * 0.5 + 0.5;

    if (projCoords.z > 1.0) return 0.0;

    vec2 texelSize = 1.0 / vec2(textureSize(shadowMap, 0).xy);

    // Minimal depth bias paired with normal-offset (in terrain.frag): keep it small because large
    // bias becomes large horizontal shadow detachment (peter-panning) at grazing sun angles, where
    // shift ≈ bias / sin(sunElevation). The shadow pass renders both faces so casters anchor at
    // their light-facing side; the normal offset clears the resulting self-shadow acne.
    float cosTheta = clamp(dot(normal, lightDir), 0.0, 1.0);
    float bias = max(0.0006 * (1.0 - cosTheta), 0.0001);

    float penumbraWidth = clamp(lightSize * projCoords.z * 80.0, 1.5, 10.0);

    float rotAngle = random(floor(projCoords.xyz * 1000.0), layer) * TWO_PI;
    float sinA = sin(rotAngle), cosA = cos(rotAngle);

    float shadow = 0.0;
    const int SAMPLES = 32;
    for (int i = 0; i < SAMPLES; i++) {
        vec2 d = fibonacciDisk(i, SAMPLES);
        vec2 offset = vec2(d.x * cosA - d.y * sinA, d.x * sinA + d.y * cosA);
        vec2 uv = projCoords.xy + offset * texelSize * penumbraWidth;
        shadow += texture(shadowMap, vec4(uv, float(layer), projCoords.z - bias));
    }
    return 1.0 - shadow / float(SAMPLES);
}

// Standard PCF shadow (original implementation)
float CalcShadow(sampler2DShadow shadowMap, vec4 fragPosLightSpace, vec3 normal, vec3 lightDir)
{
    // perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // Reversed-Z + glClipControl(ZeroToOne): conventional shadow ortho → NDC z already [0,1].
    // Only xy needs remapping to UV; z is the comparison depth as-is.
    projCoords.xy = projCoords.xy * 0.5 + 0.5;

    if(projCoords.z > 1.0)
        return 0.0;

    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);

    // Normal-offset bias
    vec3 offsetPos = projCoords + normal * (length(texelSize) * 2.0);

    float bias = max(0.001 * (1.0 - dot(normal, lightDir)), 0.0001);

    float shadow = 0.0;
    int samples = 16;

    for (int i = 0; i < samples; i++)
    {
        // Rotate poisson disk for more organic noise
        float angle = random(floor(projCoords.xyz * 1000.0), i) * 2.0 * PI;
        float s = sin(angle);
        float c = cos(angle);
        vec2 rotatedOffset = vec2(poissonDisk[i].x * c - poissonDisk[i].y * s, poissonDisk[i].x * s + poissonDisk[i].y * c);

        shadow += texture(shadowMap, vec3(offsetPos.xy + rotatedOffset * texelSize * 2.5, offsetPos.z - bias));
    }

    shadow /= float(samples);

    return 1.0 - shadow;
}

// ============================================================================
// Contact Shadows (screen-space micro-shadows)
// Note: This function is optional and requires a separate depth texture
// ============================================================================

// Commented out as it requires integration with the render pipeline
// Uncomment and integrate when adding screen-space effects
/*
float calcContactShadows(vec3 fragPos, vec3 lightDir, sampler2D depthTexture, mat4 viewProjection) {
    // Ray march in screen space to find contact shadows
    const int maxSteps = 8;
    const float stepSize = 0.05;

    vec3 rayPos = fragPos;
    float shadow = 0.0;

    for (int i = 0; i < maxSteps; i++) {
        rayPos += lightDir * stepSize;

        // Project to screen space
        vec4 projPos = viewProjection * vec4(rayPos, 1.0);
        projPos.xyz /= projPos.w;
        vec2 screenUV = projPos.xy * 0.5 + 0.5;

        // Check bounds
        if (screenUV.x < 0.0 || screenUV.x > 1.0 || screenUV.y < 0.0 || screenUV.y > 1.0)
            break;

        // Sample depth
        float sceneDepth = texture(depthTexture, screenUV).r;

        // Check for intersection
        if (projPos.z > sceneDepth) {
            shadow = 1.0;
            break;
        }
    }

    return shadow * 0.3; // Reduce intensity for subtlety
}
*/

#endif