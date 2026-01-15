namespace SharpCraft.Sdk.UI;

/// <summary>
/// Provides access to graphics settings.
/// </summary>
public interface IGraphicsSettings
{
    bool IsVisible { get; set; }
    event Action? OnVisibilityChanged;
    
    bool VSync { get; set; }
    float Gamma { get; set; }
    float Exposure { get; set; }
    
    bool UseNormalMap { get; set; }
    float NormalStrength { get; set; }
    
    bool UseAoMap { get; set; }
    float AoMapStrength { get; set; }
    
    bool UseSpecularMap { get; set; }
    float SpecularMapStrength { get; set; }
    
    float FogNearFactor { get; set; }
    float FogFarFactor { get; set; }
    int RenderDistance { get; set; }
}
