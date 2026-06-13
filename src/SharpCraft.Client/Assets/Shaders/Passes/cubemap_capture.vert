#version 450 core
layout (location = 0) in vec3 aPos;

out vec3 LocalPos;

// Combined view*projection for the current cube face (System.Numerics order, as elsewhere).
uniform mat4 viewProjection;

void main() {
    LocalPos = aPos;
    gl_Position = viewProjection * vec4(aPos, 1.0);
}
