using SharpCraft.Engine.Rendering.Lighting;
using SharpCraft.Sdk.UI;
using System.Numerics;

namespace SharpCraft.Engine.Rendering;

/// <summary>
/// Assembles a <see cref="RenderContext"/> for a frame from the per-frame camera/scene inputs and
/// the shared <see cref="IGraphicsSettings"/>, keeping the settings-to-context mapping and fog math
/// in one testable place.
/// </summary>
public static class RenderContextBuilder
{
    /// <summary>Builds the render context for a single frame.</summary>
    public static RenderContext Build(
        Matrix4x4 view,
        Matrix4x4 projection,
        Vector3 cameraPosition,
        Vector3 fogColor,
        float viewDistance,
        int screenWidth,
        int screenHeight,
        DirectionalLightData sun,
        PointLightData[] pointLights,
        bool isUnderwater,
        float time,
        IGraphicsSettings settings,
        float atmosphereRayleigh,
        float atmosphereMie,
        float atmosphereOzone,
        float atmosphereMieG)
    {
        var fogNear = isUnderwater ? 0.0f : viewDistance * settings.FogNearFactor;
        var fogFar = isUnderwater ? 20.0f : viewDistance * settings.FogFarFactor;

        return new RenderContext(
            Camera: new CameraData(view, projection, cameraPosition, screenWidth, screenHeight),
            Fog: new FogData(fogColor, fogNear, fogFar),
            Lighting: new SceneLighting(sun, pointLights),
            Pbr: new PbrSettings(
                settings.UseNormalMap, settings.NormalStrength,
                settings.UseAoMap, settings.AoMapStrength,
                settings.UseMetallicMap, settings.MetallicStrength,
                settings.UseRoughnessMap, settings.RoughnessStrength),
            Exposure: new ExposureSettings(
                settings.Exposure, settings.AutoExposureKey, settings.AutoExposureMin,
                settings.AutoExposureMax, settings.AutoExposureSpeed, settings.Gamma),
            Atmosphere: new AtmosphereSettings(atmosphereRayleigh, atmosphereMie, atmosphereOzone, atmosphereMieG),
            Effects: new EffectSettings(
                settings.UseIbl, settings.UseSsao, settings.SsaoRadius, settings.SsaoIntensity,
                settings.UseSsr, settings.UseContactShadows),
            IsUnderwater: isUnderwater,
            Time: time);
    }
}
