using SharpCraft.Engine.Rendering.Lighting;
using System.Numerics;

namespace SharpCraft.Engine.Rendering;

/// <summary>Camera transforms and the render target size for a frame.</summary>
public readonly record struct CameraData(
    Matrix4x4 View,
    Matrix4x4 Projection,
    Vector3 CameraPosition,
    int ScreenWidth,
    int ScreenHeight)
{
    public Matrix4x4 ViewProjection => View * Projection;
}

/// <summary>Distance fog colour and range for a frame.</summary>
public readonly record struct FogData(
    Vector3 FogColor,
    float FogNear,
    float FogFar);

/// <summary>PBR material map toggles and strengths.</summary>
public readonly record struct PbrSettings(
    bool UseNormalMap,
    float NormalStrength,
    bool UseAoMap,
    float AoMapStrength,
    bool UseMetallicMap,
    float MetallicStrength,
    bool UseRoughnessMap,
    float RoughnessStrength,
    bool UseSpecularMap = true,
    float SpecularMapStrength = 1f);

/// <summary>Tone-mapping exposure, eye-adaptation, and display gamma.</summary>
public readonly record struct ExposureSettings(
    float Exposure,
    float AutoExposureKey,
    float AutoExposureMin,
    float AutoExposureMax,
    float AutoExposureSpeed,
    float Gamma);

/// <summary>Atmospheric scattering scales for the sky/volumetrics.</summary>
public readonly record struct AtmosphereSettings(
    float RayleighScale,
    float MieScale,
    float OzoneScale,
    float MieG);

/// <summary>The scene's directional sun and point lights for a frame.</summary>
public readonly record struct SceneLighting(
    DirectionalLightData? Sun,
    PointLightData[]? PointLights);

/// <summary>Screen-space and image-based effect toggles/parameters.</summary>
public readonly record struct EffectSettings(
    bool UseIbl,
    bool UseSsao,
    float SsaoRadius,
    float SsaoIntensity,
    bool UseSsr,
    bool UseContactShadows,
    bool UseTaa = true,
    bool UseBloom = true);

/// <summary>
/// Everything a frame's render pipeline needs, grouped by concern. Built by
/// <see cref="RenderContextBuilder"/> from the per-frame scene state and the shared graphics settings.
/// </summary>
public readonly record struct RenderContext(
    CameraData Camera,
    FogData Fog,
    SceneLighting Lighting,
    PbrSettings Pbr,
    ExposureSettings Exposure,
    AtmosphereSettings Atmosphere,
    EffectSettings Effects,
    bool IsUnderwater,
    float Time);
