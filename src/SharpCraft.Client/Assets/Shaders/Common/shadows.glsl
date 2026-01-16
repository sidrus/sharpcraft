float CalcShadow(sampler2DShadow shadowMap, vec4 fragPosLightSpace, vec3 normal, vec3 lightDir)
{
    // perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;

    if(projCoords.z > 1.0)
        return 0.0;

    // Normal-offset bias to further reduce acne without causing massive peter-panning
    // We slightly offset the position along the normal when sampling
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
    
    // Applying a small normal offset bias
    // This pushes the lookup position slightly away from the surface
    // We scale the offset by the texel size to keep it proportional to the shadow map resolution
    vec3 offsetPos = projCoords + normal * (length(texelSize) * 1.5); 
    
    float bias = max(0.001 * (1.0 - dot(normal, lightDir)), 0.0001);
    
    // Hardware PCF (Percentage-Closer Filtering)
    float shadow = 0.0;
    
    // 3x3 kernel with hardware-accelerated bilinear filtering per sample
    for(int x = -1; x <= 1; ++x)
    {
        for(int y = -1; y <= 1; ++y)
        {
            shadow += texture(shadowMap, vec3(offsetPos.xy + vec2(x, y) * texelSize, offsetPos.z - bias));
        }    
    }
    shadow /= 9.0;
    
    // texture() returns 1.0 for "in light", so we invert it for "shadow"
    return 1.0 - shadow;
}
