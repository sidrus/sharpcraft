#version 450 core

layout (location = 0) out vec3 bloomColor;

in vec2 TexCoords;

uniform sampler2D srcTexture;
uniform float filterRadius;

void main()
{
    // The filter kernel is applied with a radius, specified in texture coordinates, so that the blur
    // width is independent of the texture size (i.e. it is always the same percentage of the screen).
    float x = filterRadius;
    float y = filterRadius;

    // Take 9 samples around current texel:
    // a - b - c
    // d - e - f
    // g - h - i
    // === 1 2 1
    // === 2 4 2
    // === 1 2 1
    vec3 a = texture(srcTexture, vec2(TexCoords.x - x, TexCoords.y + y)).rgb;
    vec3 b = texture(srcTexture, vec2(TexCoords.x,     TexCoords.y + y)).rgb;
    vec3 c = texture(srcTexture, vec2(TexCoords.x + x, TexCoords.y + y)).rgb;

    vec3 d = texture(srcTexture, vec2(TexCoords.x - x, TexCoords.y)).rgb;
    vec3 e = texture(srcTexture, vec2(TexCoords.x,     TexCoords.y)).rgb;
    vec3 f = texture(srcTexture, vec2(TexCoords.x + x, TexCoords.y)).rgb;

    vec3 g = texture(srcTexture, vec2(TexCoords.x - x, TexCoords.y - y)).rgb;
    vec3 h = texture(srcTexture, vec2(TexCoords.x,     TexCoords.y - y)).rgb;
    vec3 i = texture(srcTexture, vec2(TexCoords.x + x, TexCoords.y - y)).rgb;

    // Apply 3x3 gaussian filter:
    bloomColor = e*4.0;
    bloomColor += (b+d+f+h)*2.0;
    bloomColor += (a+c+g+i);
    bloomColor *= 1.0 / 16.0;
}
