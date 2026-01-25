#version 450 core
layout (location = 0) in vec3 aPos;

layout (std140, binding = 0) uniform SceneData {
    mat4 ViewProjection;
    vec4 ViewPos;
    vec4 FogColor;
    float FogNear;
    float FogFar;
    float Exposure;
    float Gamma;
};

out vec3 TexCoords;

void main()
{
    TexCoords = aPos;
    // Remove translation from view matrix to keep skybox centered on camera
    mat4 view = mat4(mat3(ViewProjection)); 
    // Wait, ViewProjection is already combined. I need just the view-rotation * projection.
    // However, I can reconstruct it or pass it. 
    // Actually, a simpler way for a skybox is to use the existing ViewProjection but set position to ViewPos + aPos * distance
    
    vec4 pos = ViewProjection * vec4(aPos + ViewPos.xyz, 1.0);
    gl_Position = pos.xyww; // Far plane
}
