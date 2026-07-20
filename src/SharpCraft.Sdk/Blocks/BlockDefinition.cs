using SharpCraft.Sdk.Resources;

namespace SharpCraft.Sdk.Blocks;

/// <summary>
/// Defines the properties of a block type.
/// </summary>
/// <param name="Id">The unique identifier of the block.</param>
/// <param name="Name">The display name of the block.</param>
/// <param name="Type">The logical type of the block.</param>
/// <param name="TextureTop">The texture for the top face.</param>
/// <param name="TextureBottom">The texture for the bottom face.</param>
/// <param name="TextureSides">The texture for the side faces.</param>
public record BlockDefinition(
    ResourceLocation Id,
    string Name,
    BlockType Type = BlockType.Air,
    ResourceLocation? TextureTop = null,
    ResourceLocation? TextureBottom = null,
    ResourceLocation? TextureSides = null
);
