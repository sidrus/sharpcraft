#version 450 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D screenTexture;
uniform float time;
uniform bool isUnderwater;

void main()
{
    if (!isUnderwater) {
        FragColor = texture(screenTexture, TexCoords);
        return;
    }

    // Wavy distortion
    vec2 distortedTexCoords = TexCoords;
    distortedTexCoords.x += sin(distortedTexCoords.y * 10.0 + time * 2) * 0.001;
    distortedTexCoords.y += cos(distortedTexCoords.x * 10.0 + time * 2) * 0.001;

    vec4 color = texture(screenTexture, distortedTexCoords);

    // Blue tint
    vec3 underwaterColor = vec3(0.0, 0.4, 0.8);
    color.rgb = mix(color.rgb, underwaterColor, 0.4);

    FragColor = color;
}
