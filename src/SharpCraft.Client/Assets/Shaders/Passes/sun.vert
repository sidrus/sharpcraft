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

uniform vec3 sunDir;
uniform float sunSize;

out vec2 TexCoord;

void main() {
    // We want the sun to be at "infinity", so we center it around the camera
    // and push it to the far plane by setting z = w in clip space.
    
    // Create a billboard oriented towards the sun direction
    vec3 up = abs(sunDir.y) > 0.99 ? vec3(0, 0, 1) : vec3(0, 1, 0);
    vec3 right = normalize(cross(up, sunDir));
    up = cross(sunDir, right);
    
    vec3 pos = sunDir * 100.0 + (aPos.x * right + aPos.y * up) * sunSize;
    
    TexCoord = aPos.xy * 0.5 + 0.5;
    
    vec4 clipPos = ViewProjection * vec4(pos + ViewPos.xyz, 1.0);
    gl_Position = clipPos.xyww; // Force z to be at the far plane
}
