using SharpCraft.Sdk.UI;
using System.Numerics;

namespace SharpCraft.CoreMods.UI;

/// <summary>
/// Settings panel that edits the shared <see cref="IGraphicsSettings"/> state. Owns only its own
/// panel visibility; all persisted values live on the injected settings.
/// </summary>
public class GraphicsSettingsHud(IGraphicsSettings settings) : InteractiveHud
{
    public override string Name => "GraphicsSettingsHud";

    public override void Draw(double deltaTime, IGui gui, IHudContext context)
    {
        if (!IsVisible)
        {
            return;
        }

        var viewportSize = gui.GetMainViewportSize();
        gui.SetNextWindowPos(viewportSize * 0.5f, GuiCond.Appearing, new Vector2(0.5f, 0.5f));
        gui.SetNextWindowSize(Vector2.Zero); // Auto-height

        var visible = IsVisible;
        if (gui.Begin("Graphics Settings", ref visible, GuiWindowSettings.NoCollapse | GuiWindowSettings.AlwaysAutoResize))
        {
            var useNormalMap = settings.UseNormalMap;
            var normalStrength = settings.NormalStrength;
            var useAoMap = settings.UseAoMap;
            var aoMapStrength = settings.AoMapStrength;
            var useMetallicMap = settings.UseMetallicMap;
            var metallicStrength = settings.MetallicStrength;
            var useRoughnessMap = settings.UseRoughnessMap;
            var roughnessStrength = settings.RoughnessStrength;
            var useIbl = settings.UseIbl;
            var useSsao = settings.UseSsao;
            var ssaoRadius = settings.SsaoRadius;
            var ssaoIntensity = settings.SsaoIntensity;
            var useSsr = settings.UseSsr;
            var useContactShadows = settings.UseContactShadows;
            var vSync = settings.VSync;
            var gamma = settings.Gamma;
            var exposure = settings.Exposure;
            var autoExposureKey = settings.AutoExposureKey;
            var autoExposureMin = settings.AutoExposureMin;
            var autoExposureMax = settings.AutoExposureMax;
            var autoExposureSpeed = settings.AutoExposureSpeed;
            var renderDistance = settings.RenderDistance;
            var fogNearFactor = settings.FogNearFactor;
            var fogFarFactor = settings.FogFarFactor;
            var maxPointLights = settings.MaxPointLights;

            gui.Panel("Pipeline Features", () =>
            {
                gui.Checkbox("Enable Normal Mapping", ref useNormalMap);
                gui.SliderFloat("Normal Strength", ref normalStrength, 0.0f, 10.0f);
                gui.Checkbox("Enable Ambient Occlusion", ref useAoMap);
                gui.SliderFloat("Ambient Occlusion Strength", ref aoMapStrength, 0.0f, 10.0f);
                gui.Checkbox("Enable Metallic Mapping", ref useMetallicMap);
                gui.SliderFloat("Metallic Strength", ref metallicStrength, 0.0f, 10.0f);
                gui.Checkbox("Enable Roughness Mapping", ref useRoughnessMap);
                gui.SliderFloat("Roughness Strength", ref roughnessStrength, 0.0f, 10.0f);
                gui.Checkbox("Enable IBL (Sky Lighting)", ref useIbl);
                gui.Checkbox("Enable SSAO (Ambient Occlusion)", ref useSsao);
                gui.SliderFloat("SSAO Radius", ref ssaoRadius, 0.2f, 4.0f);
                gui.SliderFloat("SSAO Intensity", ref ssaoIntensity, 0.0f, 8.0f);
                gui.Checkbox("Enable SSR (Water Reflections)", ref useSsr);
                gui.Checkbox("Enable Contact Shadows", ref useContactShadows);
                gui.SliderInt("Max Point Lights", ref maxPointLights, 1, 256);
                gui.Checkbox("VSync", ref vSync);
                gui.SliderFloat("Gamma Correction", ref gamma, 0.0f, 4.0f);
                gui.SliderFloat("Exposure Compensation", ref exposure, 0.0f, 10.0f);
            });

            gui.Panel("Auto-Exposure (Eye Adaptation)", () =>
            {
                gui.SliderFloat("Key (target gray)", ref autoExposureKey, 0.02f, 0.6f);
                gui.SliderFloat("Min Exposure", ref autoExposureMin, 0.01f, 1.0f);
                gui.SliderFloat("Max Exposure (dark cap)", ref autoExposureMax, 0.2f, 16.0f);
                gui.SliderFloat("Adaptation Speed", ref autoExposureSpeed, 0.1f, 10.0f);
            });

            gui.Panel("Atmospherics", () =>
            {
                gui.SliderInt("Render Distance", ref renderDistance, 2, 32);
                gui.SliderFloat("Fog Near Offset", ref fogNearFactor, 0.0f, 1.0f);
                gui.SliderFloat("Fog Far Offset", ref fogFarFactor, 0.1f, 2.0f);
            });

            settings.UseNormalMap = useNormalMap;
            settings.NormalStrength = normalStrength;
            settings.UseAoMap = useAoMap;
            settings.AoMapStrength = aoMapStrength;
            settings.UseMetallicMap = useMetallicMap;
            settings.MetallicStrength = metallicStrength;
            settings.UseRoughnessMap = useRoughnessMap;
            settings.RoughnessStrength = roughnessStrength;
            settings.UseIbl = useIbl;
            settings.UseSsao = useSsao;
            settings.SsaoRadius = ssaoRadius;
            settings.SsaoIntensity = ssaoIntensity;
            settings.UseSsr = useSsr;
            settings.UseContactShadows = useContactShadows;
            settings.VSync = vSync;
            settings.Gamma = gamma;
            settings.Exposure = exposure;
            settings.AutoExposureKey = autoExposureKey;
            settings.AutoExposureMin = autoExposureMin;
            settings.AutoExposureMax = autoExposureMax;
            settings.AutoExposureSpeed = autoExposureSpeed;
            settings.RenderDistance = renderDistance;
            settings.FogNearFactor = fogNearFactor;
            settings.FogFarFactor = fogFarFactor;
            settings.MaxPointLights = maxPointLights;

            gui.Spacing();
            gui.Separator();

            if (gui.Button("Close", new Vector2(-1, 0)))
            {
                visible = false;
            }

            gui.End();
        }

        if (IsVisible != visible)
        {
            IsVisible = visible;
        }
    }
}