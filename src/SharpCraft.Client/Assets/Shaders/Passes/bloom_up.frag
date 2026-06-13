#version 450 core

// Bloom upsample (research §5.6): 3x3 tent filter on the lower mip, additively accumulated into the
// next-larger mip (the pipeline enables additive blending for this pass). Progressive upsampling up
// the pyramid yields a wide, resolution-independent, energy-preserving bloom.

out vec3 FragColor;
in vec2 TexCoords;

uniform sampler2D srcTexture;  // the smaller mip being upsampled
uniform vec2 srcTexelSize;     // texel size of srcTexture

void main() {
    vec2 t = srcTexelSize;
    vec2 uv = TexCoords;

    vec3 a = texture(srcTexture, uv + t * vec2(-1.0,  1.0)).rgb;
    vec3 b = texture(srcTexture, uv + t * vec2( 0.0,  1.0)).rgb;
    vec3 c = texture(srcTexture, uv + t * vec2( 1.0,  1.0)).rgb;
    vec3 d = texture(srcTexture, uv + t * vec2(-1.0,  0.0)).rgb;
    vec3 e = texture(srcTexture, uv).rgb;
    vec3 f = texture(srcTexture, uv + t * vec2( 1.0,  0.0)).rgb;
    vec3 g = texture(srcTexture, uv + t * vec2(-1.0, -1.0)).rgb;
    vec3 h = texture(srcTexture, uv + t * vec2( 0.0, -1.0)).rgb;
    vec3 i = texture(srcTexture, uv + t * vec2( 1.0, -1.0)).rgb;

    vec3 result = e * 4.0 + (b + d + f + h) * 2.0 + (a + c + g + i);
    FragColor = result * (1.0 / 16.0);
}
