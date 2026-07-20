namespace SharpCraft.Sdk.Lifecycle;

/// <summary>
/// Defines a SharpCraft mod.
/// </summary>
public interface IMod
{
    /// <summary>
    /// Gets the mod manifest.
    /// </summary>
    ModManifest Manifest { get; }

    /// <summary>
    /// Gets the base directory of the mod.
    /// </summary>
    string BaseDirectory { get; set; }

    /// <summary>
    /// Called when the mod is enabled.
    /// </summary>
    void OnEnable();

    /// <summary>
    /// Called when the mod is disabled.
    /// </summary>
    void OnDisable();
}