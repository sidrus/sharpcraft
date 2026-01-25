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

float CalcShadow(sampler2DShadow shadowMap, vec4 fragPosLightSpace, vec3 normal, vec3 lightDir)
{
    // perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;

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

#endif