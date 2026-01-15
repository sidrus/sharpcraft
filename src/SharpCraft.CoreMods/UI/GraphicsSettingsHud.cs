using System.Numerics;
using SharpCraft.Sdk.UI;

namespace SharpCraft.CoreMods.UI;

/// <summary>
/// Graphics settings menu.
/// </summary>
public class GraphicsSettingsHud : IHud, IGraphicsSettings
{
    public string Name => "GraphicsSettingsHud";

    private bool _isVisible;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value) return;
            _isVisible = value;
            OnVisibilityChanged?.Invoke();
        }
    }

    private bool _useNormalMap = true;
    public bool UseNormalMap { get => _useNormalMap; set => _useNormalMap = value; }

    private float _normalStrength = 0.5f;
    public float NormalStrength { get => _normalStrength; set => _normalStrength = value; }

    private bool _useAoMap = true;
    public bool UseAoMap { get => _useAoMap; set => _useAoMap = value; }

    private float _aoMapStrength = 0.5f;
    public float AoMapStrength { get => _aoMapStrength; set => _aoMapStrength = value; }

    private bool _useMetallicMap = true;
    public bool UseMetallicMap { get => _useMetallicMap; set => _useMetallicMap = value; }

    private float _metallicStrength = 1.0f;
    public float MetallicStrength { get => _metallicStrength; set => _metallicStrength = value; }

    private bool _useRoughnessMap = true;
    public bool UseRoughnessMap { get => _useRoughnessMap; set => _useRoughnessMap = value; }

    private float _roughnessStrength = 1.0f;
    public float RoughnessStrength { get => _roughnessStrength; set => _roughnessStrength = value; }

    private bool _useIBL = false;
    public bool UseIBL { get => _useIBL; set => _useIBL = value; }

    private bool _vSync = false;
    public bool VSync { get => _vSync; set => _vSync = value; }

    private float _gamma = 1.6f;
    public float Gamma { get => _gamma; set => _gamma = value; }

    private float _exposure = 1.0f;
    public float Exposure { get => _exposure; set => _exposure = value; }

    private float _fogNearFactor = 0.3f;
    public float FogNearFactor { get => _fogNearFactor; set => _fogNearFactor = value; }

    private float _fogFarFactor = 0.95f;
    public float FogFarFactor { get => _fogFarFactor; set => _fogFarFactor = value; }

    private int _renderDistance = 8;
    public int RenderDistance { get => _renderDistance; set => _renderDistance = value; }

    public event Action? OnVisibilityChanged;

    public void Draw(double deltaTime, IGui gui, IHudContext context)
    {
        if (!IsVisible) return;

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
                gui.Checkbox("Enable IBL (Approximation)", ref _useIBL);
                gui.Checkbox("VSync", ref _vSync);
                gui.SliderFloat("Gamma Correction", ref _gamma, 0.0f, 4.0f);
                gui.SliderFloat("Exposure", ref _exposure, 0.0f, 10.0f);
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

    public void OnAwake() { }
    public void OnUpdate(double deltaTime) { }
}
