#version 450 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aUv;
layout (location = 2) in vec3 aNorm;

out vec3 Normal;
out vec3 FragPos;
out vec2 TexCoord;
out mat3 TBN;

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

    vec3 normal = normalize(mat3(model) * aNorm);
    Normal = normal;

    // Tangent basis for axis-aligned voxel faces (the mesh carries no tangents).
    vec3 tangent = abs(normal.y) > 0.5 ? vec3(1.0, 0.0, 0.0)
                 : abs(normal.z) > 0.5 ? vec3(1.0, 0.0, 0.0)
                 : vec3(0.0, 0.0, 1.0);
    tangent = normalize(tangent - dot(tangent, normal) * normal);
    vec3 bitangent = cross(normal, tangent);
    TBN = mat3(tangent, bitangent, normal);

    TexCoord = aUv;
    gl_Position = ViewProjection * worldPos; // reversed-Z projection
}
