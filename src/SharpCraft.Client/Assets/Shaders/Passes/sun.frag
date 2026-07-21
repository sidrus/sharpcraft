#version 450 core
out vec4 FragColor;

in vec2 TexCoord;
in vec3 WorldDir;

uniform vec3 sunColor;
uniform float sunIntensity;
uniform float sunElevation;   // sin(elevation); warmer + dimmer near the horizon
uniform float elevationFade;  // 0 below horizon → 1 once risen

void main() {
    // Clip the disc at the horizon so it's occluded by the ground/sea instead of floating below it
    // (gives a natural half-sun at sunrise/sunset). A tiny softening avoids a hard aliased cut.
    float horizon = smoothstep(-0.004, 0.004, normalize(WorldDir).y);
    if (horizon <= 0.0) discard;

    float dist = length(TexCoord - vec2(0.5));
    if (dist > 0.5) discard;

    // Soft edge for the sun
    float alpha = smoothstep(0.5, 0.49, dist) * elevationFade * horizon;

    // Warm/red at the horizon, sun-colored higher up — a color shift only, NOT a brightness crush
    // (heavy extinction made the disc's outer ring darker than the bright horizon glow → "black
    // edges"). The disc stays well above the sky so it always reads as the sun.
    vec3 tint = mix(vec3(1.0, 0.5, 0.25), sunColor, smoothstep(0.0, 0.25, sunElevation));

    // Bright disc, well ABOVE the sky radiance (which is scaled by SKY_ILLUMINANCE) so it always
    // reads as the sun rather than a dark blob against the bright sky — brighter core for bloom.
    float core = smoothstep(0.45, 0.0, dist);
    float brightness = 30.0 + core * 70.0;
    vec3 color = tint * brightness;

    FragColor = vec4(color, alpha);
}
