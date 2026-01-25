#version 450 core

layout (location = 0) out vec3 bloomColor;

in vec2 TexCoords;

uniform sampler2D srcTexture;
uniform vec2 srcResolution;
uniform float bloomThreshold = 1.0;

// Soft threshold function to avoid harsh cutoffs
vec3 applyThreshold(vec3 color) {
    float brightness = max(color.r, max(color.g, color.b));
    float soft = brightness - bloomThreshold + 0.5;
    soft = clamp(soft, 0.0, 1.0);
    soft = soft * soft * (3.0 - 2.0 * soft); // smoothstep
    float contribution = max(0.0, brightness - bloomThreshold) / max(brightness, 0.0001);
    return color * mix(contribution, 1.0, soft) * step(bloomThreshold * 0.5, brightness);
}

void main()
{
    vec2 srcTexelSize = 1.0 / srcResolution;
    float x = srcTexelSize.x;
    float y = srcTexelSize.y;

    // Take 13 samples around current texel:
    // a - b - c
    // - d - e -
    // f - g - h
    // - i - j -
    // k - l - m

    // [0,0]
    vec3 e = texture(srcTexture, vec2(TexCoords.x,     TexCoords.y)).rgb;
    vec3 a = texture(srcTexture, vec2(TexCoords.x - 2*x, TexCoords.y + 2*y)).rgb;
    vec3 b = texture(srcTexture, vec2(TexCoords.x,     TexCoords.y + 2*y)).rgb;
    vec3 c = texture(srcTexture, vec2(TexCoords.x + 2*x, TexCoords.y + 2*y)).rgb;
    vec3 d = texture(srcTexture, vec2(TexCoords.x - x,   TexCoords.y + y)).rgb;
    vec3 f = texture(srcTexture, vec2(TexCoords.x - 2*x, TexCoords.y)).rgb;
    vec3 g = texture(srcTexture, vec2(TexCoords.x + 2*x, TexCoords.y)).rgb;
    vec3 h = texture(srcTexture, vec2(TexCoords.x - x,   TexCoords.y - y)).rgb;
    vec3 i = texture(srcTexture, vec2(TexCoords.x + x,   TexCoords.y + y)).rgb;
    vec3 j = texture(srcTexture, vec2(TexCoords.x + x,   TexCoords.y - y)).rgb;
    vec3 k = texture(srcTexture, vec2(TexCoords.x - 2*x, TexCoords.y - 2*y)).rgb;
    vec3 l = texture(srcTexture, vec2(TexCoords.x,     TexCoords.y - 2*y)).rgb;
    vec3 m = texture(srcTexture, vec2(TexCoords.x + 2*x, TexCoords.y - 2*y)).rgb;

    // Apply weighted distribution:
    // 0.5 + 0.125 + 0.125 + 0.125 + 0.125 = 1
    // a,b,c,d,e,f,g,h,i,j,k,l,m
    bloomColor = e * 0.125;
    bloomColor += (a+c+k+m) * 0.03125;
    bloomColor += (b+f+g+l) * 0.0625;
    bloomColor += (d+h+i+j) * 0.125;
    
    // Apply threshold to extract only bright pixels for bloom
    bloomColor = applyThreshold(bloomColor);
}
