namespace SharpCraft.Sdk.UI;

/// <summary>
/// Canonical <see cref="IGraphicsSettings"/> state with the default photoreal values. Single source
/// of truth for those defaults, shared by reference between the render pipeline and the settings
/// panel that edits it — so it is a class, not a value type.
/// </summary>
public sealed class GraphicsSettings : IGraphicsSettings
{
    public bool VSync { get; set; }

    // Standard sRGB display gamma; the HDR chain is linear until the final pass.
    public float Gamma { get; set; } = 2.2f;
    public float Exposure { get; set; } = 1.0f;

    // Auto-exposure / eye adaptation (research §5.2).
    public float AutoExposureKey { get; set; } = 0.18f;
    public float AutoExposureMin { get; set; } = 0.05f;
    public float AutoExposureMax { get; set; } = 2.0f;
    public float AutoExposureSpeed { get; set; } = 2.5f;

    public bool UseNormalMap { get; set; } = true;
    public float NormalStrength { get; set; } = 1.0f;
    public bool UseAoMap { get; set; } = true;
    public float AoMapStrength { get; set; } = 0.8f;
    public bool UseMetallicMap { get; set; } = true;
    public float MetallicStrength { get; set; } = 1.0f;
    public bool UseRoughnessMap { get; set; } = true;
    public float RoughnessStrength { get; set; } = 1.0f;

    public bool UseIbl { get; set; } = true;
    public bool UseSsao { get; set; } = true;
    public float SsaoRadius { get; set; } = 1.5f;
    public float SsaoIntensity { get; set; } = 2.5f;
    public bool UseSsr { get; set; } = true;
    public bool UseContactShadows { get; set; } = true;

    public float FogNearFactor { get; set; } = 0.3f;
    public float FogFarFactor { get; set; } = 0.95f;
    public int RenderDistance { get; set; } = 8;
}
