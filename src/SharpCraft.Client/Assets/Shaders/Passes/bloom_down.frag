#version 450 core

// Bloom downsample (research §5.6): the Call of Duty / Jimenez 13-tap progressive downsample. On the
// first pass a Karis average (weight each box by 1/(1+luma)) plus an optional soft-knee prefilter
// stops single bright pixels from becoming firefly flares.

out vec3 FragColor;
in vec2 TexCoords;

uniform sampler2D srcTexture;
uniform vec2 srcTexelSize;
uniform int firstPass;     // 1 → Karis average + prefilter
uniform float threshold;   // soft-knee bloom threshold (first pass only)

float luma(vec3 c) { return dot(c, vec3(0.2126, 0.7152, 0.0722)); }

// Soft-knee high-pass so dim values fade in gradually instead of a hard cutoff.
vec3 prefilter(vec3 c) {
    float knee = max(threshold * 0.5, 1e-4);
    float br = max(c.r, max(c.g, c.b));
    float soft = clamp(br - threshold + knee, 0.0, 2.0 * knee);
    soft = soft * soft / (4.0 * knee);
    float contrib = max(soft, br - threshold) / max(br, 1e-4);
    return c * max(contrib, 0.0);
}

void main() {
    vec2 t = srcTexelSize;
    vec2 uv = TexCoords;

    vec3 a = texture(srcTexture, uv + t * vec2(-2.0,  2.0)).rgb;
    vec3 b = texture(srcTexture, uv + t * vec2( 0.0,  2.0)).rgb;
    vec3 c = texture(srcTexture, uv + t * vec2( 2.0,  2.0)).rgb;
    vec3 d = texture(srcTexture, uv + t * vec2(-2.0,  0.0)).rgb;
    vec3 e = texture(srcTexture, uv).rgb;
    vec3 f = texture(srcTexture, uv + t * vec2( 2.0,  0.0)).rgb;
    vec3 g = texture(srcTexture, uv + t * vec2(-2.0, -2.0)).rgb;
    vec3 h = texture(srcTexture, uv + t * vec2( 0.0, -2.0)).rgb;
    vec3 i = texture(srcTexture, uv + t * vec2( 2.0, -2.0)).rgb;
    vec3 j = texture(srcTexture, uv + t * vec2(-1.0,  1.0)).rgb;
    vec3 k = texture(srcTexture, uv + t * vec2( 1.0,  1.0)).rgb;
    vec3 l = texture(srcTexture, uv + t * vec2(-1.0, -1.0)).rgb;
    vec3 m = texture(srcTexture, uv + t * vec2( 1.0, -1.0)).rgb;

    vec3 result;
    if (firstPass == 1) {
        // Five overlapping 2x2 boxes, each Karis-weighted to suppress fireflies.
        vec3 g0 = (a + b + d + e) * 0.25;
        vec3 g1 = (b + c + e + f) * 0.25;
        vec3 g2 = (d + e + g + h) * 0.25;
        vec3 g3 = (e + f + h + i) * 0.25;
        vec3 g4 = (j + k + l + m) * 0.25;
        float w0 = 1.0 / (1.0 + luma(g0));
        float w1 = 1.0 / (1.0 + luma(g1));
        float w2 = 1.0 / (1.0 + luma(g2));
        float w3 = 1.0 / (1.0 + luma(g3));
        float w4 = 1.0 / (1.0 + luma(g4));
        result = (g0*w0 + g1*w1 + g2*w2 + g3*w3 + g4*w4) / (w0 + w1 + w2 + w3 + w4);
        result = prefilter(result);
    } else {
        result  = e * 0.125;
        result += (a + c + g + i) * 0.03125;
        result += (b + d + f + h) * 0.0625;
        result += (j + k + l + m) * 0.125;
    }
    FragColor = max(result, vec3(0.0));
}
