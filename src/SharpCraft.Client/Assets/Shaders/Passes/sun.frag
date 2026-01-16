#version 450 core
out vec4 FragColor;

in vec2 TexCoord;

uniform vec3 sunColor;
uniform float sunIntensity;

void main() {
    float dist = length(TexCoord - vec2(0.5));
    if (dist > 0.5) discard;
    
    // Soft edge for the sun
    float alpha = smoothstep(0.5, 0.49, dist);
    
    // Core of the sun is very bright
    float core = smoothstep(0.45, 0.0, dist);
    vec3 color = sunColor * sunIntensity;
    color += core * sunIntensity * 2.0;
    
    FragColor = vec4(color, alpha);
}
