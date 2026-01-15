#version 450 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aUv;
layout (location = 2) in vec3 aNorm;

out vec3 Normal;
out vec3 FragPos;
out float FragDistance;
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
};

uniform mat4 model;

void main() {
    vec4 worldPos = model * vec4(aPos, 1.0);
    FragPos = worldPos.xyz;

    vec3 normal = normalize(mat3(model) * aNorm);
    Normal = normal;

    // Improved TBN for axis-aligned voxels
    vec3 tangent;
    if (abs(normal.y) > 0.5) {
        // Top/Bottom faces
        tangent = vec3(1.0, 0.0, 0.0);
    } else if (abs(normal.z) > 0.5) {
        // Front/Back faces
        tangent = vec3(1.0, 0.0, 0.0);
    } else {
        // Left/Right faces
        tangent = vec3(0.0, 0.0, 1.0);
    }

    tangent = normalize(tangent - dot(tangent, normal) * normal);
    vec3 bitangent = cross(normal, tangent);
    TBN = mat3(tangent, bitangent, normal);

    FragDistance = length(ViewPos.xyz - FragPos);
    TexCoord = aUv;
    gl_Position = ViewProjection * worldPos;
}