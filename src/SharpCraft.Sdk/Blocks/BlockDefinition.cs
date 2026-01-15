using SharpCraft.Sdk.Resources;

namespace SharpCraft.Sdk.Blocks;

/// <summary>
/// Defines the properties of a block type.
/// </summary>
public record BlockDefinition(
    string Id,
    string Name,
    BlockType Type = BlockType.Air,
    bool IsSolid = true,
    bool IsTransparent = false,
    float Friction = 0.5f,
    ResourceLocation? TextureTop = null,
    ResourceLocation? TextureBottom = null,
    ResourceLocation? TextureSides = null
);
