#version 450 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aUv;
layout (location = 2) in vec3 aNorm;

out vec2 TexCoord;
out vec3 Normal;
out vec3 FragPos;

layout (std140, binding = 0) uniform SceneData {
    mat4 ViewProjection;
    vec4 ViewPos;
    vec4 FogColor;
    float FogNear;
    float FogFar;
    float Exposure;
    float Gamma;
    mat4 View;
};

uniform mat4 model;

void main() {
    vec4 worldPos = model * vec4(aPos, 1.0);
    FragPos = worldPos.xyz;
    Normal = normalize(mat3(model) * aNorm);
    TexCoord = aUv;
    gl_Position = ViewProjection * worldPos; // reversed-Z projection
}
