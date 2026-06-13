#version 450 core

// Chunk mesh layout (see RenderableChunk.Draw): pos(3), uv(2), normal(3)
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aNormal;

out vec3 FragPos;
out vec3 Normal;
out vec2 TexCoord;
out mat3 TBN;
out float FragDistance;
out vec4 ClipSpace;

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
uniform float time;

void main()
{
    vec4 worldPos = model * vec4(aPos, 1.0);

    // Gentle wave animation
    float waveHeight = sin(worldPos.x * 0.5 + time * 2.0) * 0.02 +
                       sin(worldPos.z * 0.7 + time * 1.5) * 0.015 +
                       sin((worldPos.x + worldPos.z) * 0.3 + time * 2.5) * 0.01;
    worldPos.y += waveHeight;

    FragPos = worldPos.xyz;
    TexCoord = aTexCoord;
    FragDistance = length(ViewPos.xyz - worldPos.xyz);

    // The chunk mesh has no tangent attributes; derive an orthonormal TBN from
    // the face normal (water faces are axis-aligned, same approach as gbuffer.vert)
    vec3 normal = normalize(mat3(model) * aNormal);
    Normal = normal;

    vec3 tangent;
    if (abs(normal.y) > 0.5) {
        tangent = vec3(1.0, 0.0, 0.0); // Top/bottom faces
    } else if (abs(normal.z) > 0.5) {
        tangent = vec3(1.0, 0.0, 0.0); // North/south faces
    } else {
        tangent = vec3(0.0, 0.0, 1.0); // East/west faces
    }

    tangent = normalize(tangent - dot(tangent, normal) * normal);
    vec3 bitangent = cross(normal, tangent);
    TBN = mat3(tangent, bitangent, normal);

    ClipSpace = ViewProjection * worldPos;
    gl_Position = ClipSpace;
}
