namespace SharpCraft.Sdk.Lifecycle;

/// <summary>
/// Metadata for a mod.
/// </summary>
/// <param name="Id">The unique identifier of the mod.</param>
/// <param name="Name">The display name of the mod.</param>
/// <param name="Author">The author of the mod.</param>
/// <param name="Version">The version of the mod.</param>
/// <param name="Dependencies">A list of mod IDs that this mod depends on.</param>
/// <param name="Capabilities">A list of capabilities this mod provides.</param>
/// <param name="Entrypoints">A list of entry points (DLLs or scripts) for this mod.</param>
public record ModManifest(
    string Id,
    string Name,
    string Author,
    string Version,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Entrypoints
);
