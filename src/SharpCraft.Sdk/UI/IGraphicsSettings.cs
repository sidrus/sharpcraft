namespace SharpCraft.Sdk.UI;

/// <summary>
/// Mutable render/graphics state, shared between the settings panel that edits it and the render
/// pipeline that reads it. Pure state: it is not a HUD and owns no drawing or visibility concerns.
/// </summary>
public interface IGraphicsSettings
{
    bool VSync
    {
        get; set;
    }
    float Gamma
    {
        get; set;
    }
    float Exposure
    {
        get; set;
    }

    // Auto-exposure / eye adaptation (research §5.2).
    float AutoExposureKey
    {
        get; set;
    }      // target middle-gray (~0.18)
    float AutoExposureMin
    {
        get; set;
    }      // lowest exposure (bright-scene floor)
    float AutoExposureMax
    {
        get; set;
    }      // highest exposure (dark-scene cap; low keeps night dark)
    float AutoExposureSpeed
    {
        get; set;
    }    // adaptation rate (higher = snappier)

    bool UseNormalMap
    {
        get; set;
    }
    float NormalStrength
    {
        get; set;
    }

    bool UseAoMap
    {
        get; set;
    }
    float AoMapStrength
    {
        get; set;
    }

    bool UseMetallicMap
    {
        get; set;
    }
    float MetallicStrength
    {
        get; set;
    }

    bool UseRoughnessMap
    {
        get; set;
    }
    float RoughnessStrength
    {
        get; set;
    }

    bool UseIbl
    {
        get; set;
    }

    bool UseSsao
    {
        get; set;
    }
    float SsaoRadius
    {
        get; set;
    }      // view-space sampling radius (world units)
    float SsaoIntensity
    {
        get; set;
    }   // AO strength multiplier

    bool UseSsr
    {
        get; set;
    }           // screen-space reflections (water)
    bool UseContactShadows
    {
        get; set;
    }// screen-space contact shadows

    float FogNearFactor
    {
        get; set;
    }
    float FogFarFactor
    {
        get; set;
    }
    int RenderDistance
    {
        get; set;
    }

    /// <summary>Maximum number of point lights considered by the clustered lighting pass.</summary>
    int MaxPointLights
    {
        get; set;
    }
}