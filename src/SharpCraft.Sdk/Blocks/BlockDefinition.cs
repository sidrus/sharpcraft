namespace SharpCraft.Sdk.Blocks;

/// <summary>
/// Defines the properties of a block type.
/// </summary>
public record BlockDefinition(
    string Id,
    string Name,
    bool IsSolid = true,
    bool IsTransparent = false,
    float Friction = 0.5f,
    string? TextureTop = null,
    string? TextureBottom = null,
    string? TextureSides = null
);
