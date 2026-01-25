#version 450 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoord;
layout (location = 3) in vec3 aTangent;
layout (location = 4) in vec3 aBitangent;

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
    Normal = mat3(transpose(inverse(model))) * aNormal;
    TexCoord = aTexCoord;
    FragDistance = length(ViewPos.xyz - worldPos.xyz);
    
    // Calculate animated TBN for normal mapping
    vec3 T = normalize(mat3(model) * aTangent);
    vec3 B = normalize(mat3(model) * aBitangent);
    vec3 N = normalize(mat3(model) * aNormal);
    TBN = mat3(T, B, N);
    
    ClipSpace = ViewProjection * worldPos;
    gl_Position = ClipSpace;
}
