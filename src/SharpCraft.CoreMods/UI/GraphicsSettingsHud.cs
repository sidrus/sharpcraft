using SharpCraft.Sdk.UI;
using System.Numerics;

namespace SharpCraft.CoreMods.UI;

/// <summary>
/// Graphics settings menu.
/// </summary>
public class GraphicsSettingsHud : IGraphicsSettings
{
    public string Name => "GraphicsSettingsHud";

    public bool IsVisible
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnVisibilityChanged?.Invoke();
        }
    }

    private bool _useNormalMap = true;
    public bool UseNormalMap
    {
        get => _useNormalMap; set => _useNormalMap = value;
    }

    private float _normalStrength = 1.0f;
    public float NormalStrength
    {
        get => _normalStrength; set => _normalStrength = value;
    }

    private bool _useAoMap = true;
    public bool UseAoMap
    {
        get => _useAoMap; set => _useAoMap = value;
    }

    private float _aoMapStrength = 0.8f;
    public float AoMapStrength
    {
        get => _aoMapStrength; set => _aoMapStrength = value;
    }

    private bool _useMetallicMap = true;
    public bool UseMetallicMap
    {
        get => _useMetallicMap; set => _useMetallicMap = value;
    }

    private float _metallicStrength = 1.0f;
    public float MetallicStrength
    {
        get => _metallicStrength; set => _metallicStrength = value;
    }

    private bool _useRoughnessMap = true;
    public bool UseRoughnessMap
    {
        get => _useRoughnessMap; set => _useRoughnessMap = value;
    }

    private float _roughnessStrength = 1.0f;
    public float RoughnessStrength
    {
        get => _roughnessStrength; set => _roughnessStrength = value;
    }

    // Image-based lighting from the baked procedural sky — the primary ambient
    // light source for the PBR pipeline. Off only as a debugging fallback.
    private bool _useIbl = true;
    public bool UseIbl
    {
        get => _useIbl; set => _useIbl = value;
    }

    // Screen-space ambient occlusion — adds contact shadows in creases/under ledges.
    private bool _useSsao = true;
    public bool UseSsao
    {
        get => _useSsao; set => _useSsao = value;
    }

    private float _ssaoRadius = 1.5f;
    public float SsaoRadius
    {
        get => _ssaoRadius; set => _ssaoRadius = value;
    }

    private float _ssaoIntensity = 2.5f;
    public float SsaoIntensity
    {
        get => _ssaoIntensity; set => _ssaoIntensity = value;
    }

    private bool _useSsr = true;
    public bool UseSsr
    {
        get => _useSsr; set => _useSsr = value;
    }

    private bool _useContactShadows = true;
    public bool UseContactShadows
    {
        get => _useContactShadows; set => _useContactShadows = value;
    }

    private bool _vSync;
    public bool VSync
    {
        get => _vSync; set => _vSync = value;
    }

    // Standard sRGB display gamma; the HDR chain is linear until the final pass
    private float _gamma = 2.2f;
    public float Gamma
    {
        get => _gamma; set => _gamma = value;
    }

    private float _exposure = 1.0f;
    public float Exposure
    {
        get => _exposure; set => _exposure = value;
    }

    // Auto-exposure / eye adaptation (research §5.2).
    private float _autoExposureKey = 0.18f;
    public float AutoExposureKey
    {
        get => _autoExposureKey; set => _autoExposureKey = value;
    }

    private float _autoExposureMin = 0.05f;
    public float AutoExposureMin
    {
        get => _autoExposureMin; set => _autoExposureMin = value;
    }

    private float _autoExposureMax = 2.0f;
    public float AutoExposureMax
    {
        get => _autoExposureMax; set => _autoExposureMax = value;
    }

    private float _autoExposureSpeed = 2.5f;
    public float AutoExposureSpeed
    {
        get => _autoExposureSpeed; set => _autoExposureSpeed = value;
    }

    private float _fogNearFactor = 0.3f;
    public float FogNearFactor
    {
        get => _fogNearFactor; set => _fogNearFactor = value;
    }

    private float _fogFarFactor = 0.95f;
    public float FogFarFactor
    {
        get => _fogFarFactor; set => _fogFarFactor = value;
    }

    private int _renderDistance = 8;
    public int RenderDistance
    {
        get => _renderDistance; set => _renderDistance = value;
    }

    public event Action? OnVisibilityChanged;

    public void Draw(double deltaTime, IGui gui, IHudContext context)
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
            gui.Panel("Pipeline Features", () =>
            {
                gui.Checkbox("Enable Normal Mapping", ref _useNormalMap);
                gui.SliderFloat("Normal Strength", ref _normalStrength, 0.0f, 10.0f);
                gui.Checkbox("Enable Ambient Occlusion", ref _useAoMap);
                gui.SliderFloat("Ambient Occlusion Strength", ref _aoMapStrength, 0.0f, 10.0f);
                gui.Checkbox("Enable Metallic Mapping", ref _useMetallicMap);
                gui.SliderFloat("Metallic Strength", ref _metallicStrength, 0.0f, 10.0f);
                gui.Checkbox("Enable Roughness Mapping", ref _useRoughnessMap);
                gui.SliderFloat("Roughness Strength", ref _roughnessStrength, 0.0f, 10.0f);
                gui.Checkbox("Enable IBL (Sky Lighting)", ref _useIbl);
                gui.Checkbox("Enable SSAO (Ambient Occlusion)", ref _useSsao);
                gui.SliderFloat("SSAO Radius", ref _ssaoRadius, 0.2f, 4.0f);
                gui.SliderFloat("SSAO Intensity", ref _ssaoIntensity, 0.0f, 8.0f);
                gui.Checkbox("Enable SSR (Water Reflections)", ref _useSsr);
                gui.Checkbox("Enable Contact Shadows", ref _useContactShadows);
                gui.Checkbox("VSync", ref _vSync);
                gui.SliderFloat("Gamma Correction", ref _gamma, 0.0f, 4.0f);
                gui.SliderFloat("Exposure Compensation", ref _exposure, 0.0f, 10.0f);
            });

            gui.Panel("Auto-Exposure (Eye Adaptation)", () =>
            {
                gui.SliderFloat("Key (target grey)", ref _autoExposureKey, 0.02f, 0.6f);
                gui.SliderFloat("Min Exposure", ref _autoExposureMin, 0.01f, 1.0f);
                gui.SliderFloat("Max Exposure (dark cap)", ref _autoExposureMax, 0.2f, 16.0f);
                gui.SliderFloat("Adaptation Speed", ref _autoExposureSpeed, 0.1f, 10.0f);
            });

            gui.Panel("Atmospherics", () =>
            {
                gui.SliderInt("Render Distance", ref _renderDistance, 2, 32);
                gui.SliderFloat("Fog Near Offset", ref _fogNearFactor, 0.0f, 1.0f);
                gui.SliderFloat("Fog Far Offset", ref _fogFarFactor, 0.1f, 2.0f);
            });

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

    public void OnAwake()
    {
    }
    public void OnUpdate(double deltaTime)
    {
    }
}