#version 450 core
layout (location = 0) in vec2 aPos; // fullscreen triangle in clip space

out vec2 Ndc;

void main()
{
    Ndc = aPos;
    // Depth is irrelevant — the sky pass runs with the depth test off. Per-pixel view rays are
    // reconstructed in the fragment shader (no cube-face interpolation crease).
    gl_Position = vec4(aPos, 0.0, 1.0);
}
