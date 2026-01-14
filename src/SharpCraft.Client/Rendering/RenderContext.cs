using System.Numerics;
using SharpCraft.Client.Rendering.Lighting;

namespace SharpCraft.Client.Rendering;

public readonly record struct RenderContext(
    Matrix4x4 View,
    Matrix4x4 Projection,
    Vector3 CameraPosition,
    Vector3 FogColor,
    float FogNear,
    float FogFar,
    int ScreenWidth,
    int ScreenHeight,
    bool UseNormalMap = true,
    float NormalStrength = 1f,
    bool UseAoMap = true,
    float AoMapStrength = 2f,
    bool UseSpecularMap = true,
    float SpecularMapStrength = 1f,
    PointLightData[]? PointLights = null,
    float Exposure = 1.0f,
    float Gamma = 1.6f
)
{
    public Matrix4x4 ViewProjection => View * Projection;
}