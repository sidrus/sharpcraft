using System.Numerics;
using SharpCraft.Engine.Rendering.Lighting;

namespace SharpCraft.Engine.Rendering;

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
    DirectionalLightData? Sun = null,
    PointLightData[]? PointLights = null,
    float Exposure = 1.0f,
    float AutoExposureKey = 0.18f,
    float AutoExposureMin = 0.05f,
    float AutoExposureMax = 2.0f,
    float AutoExposureSpeed = 2.5f,
    float Gamma = 1.6f,
    bool IsUnderwater = false,
    float Time = 0.0f,
    bool UseIBL = false,
    bool UseClusteredLighting = false,
    bool UseTAA = true,
    bool UseSSAO = true,
    float SsaoRadius = 1.5f,
    float SsaoIntensity = 2.5f,
    bool UseSSR = true,
    bool UseContactShadows = true,
    bool UseBloom = true,
    uint OpaqueColorTexture = 0,
    uint SceneDepthTexture = 0,
    Matrix4x4 InvViewProj = default,
    uint IrradianceMap = 0,
    uint PrefilterMap = 0,
    uint BrdfLut = 0,
    uint ShadowMap = 0,
    uint GtaoTexture = 0,
    float AtmosphereRayleighScale = 1.0f,
    float AtmosphereMieScale = 1.0f,
    float AtmosphereOzoneScale = 1.0f,
    float AtmosphereMieG = 0.8f
)
{
    public Matrix4x4 ViewProjection => View * Projection;
}