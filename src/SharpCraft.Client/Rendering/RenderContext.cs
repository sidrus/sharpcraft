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
    bool UseMetallicMap = true,
    float MetallicStrength = 1f,
    bool UseRoughnessMap = true,
    float RoughnessStrength = 1f,
    PointLightData[]? PointLights = null,
    float Exposure = 1.0f,
    float Gamma = 1.6f,
    bool IsUnderwater = false,
    float Time = 0.0f,
    bool UseIBL = false,
    uint IrradianceMap = 0,
    uint PrefilterMap = 0,
    uint BrdfLut = 0
)
{
    public Matrix4x4 ViewProjection => View * Projection;
}