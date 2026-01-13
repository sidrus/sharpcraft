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
    /// Called when the mod is enabled.
    /// </summary>
    void OnEnable();

    /// <summary>
    /// Called when the mod is disabled.
    /// </summary>
    void OnDisable();
}

/// <summary>
/// Metadata for a mod.
/// </summary>
public record ModManifest(
    string Id,
    string Name,
    string Author,
    string Version,
    string[] Dependencies,
    string[] Capabilities
);
