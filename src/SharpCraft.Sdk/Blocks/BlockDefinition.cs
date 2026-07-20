using SharpCraft.Sdk.Resources;

namespace SharpCraft.Sdk.Blocks;

/// <summary>
/// Defines the properties of a block. All traits are data, so mods add blocks without engine changes.
/// </summary>
public record BlockDefinition(
    ResourceLocation Id,
    string Name,
    BlockFlags Flags = BlockFlags.Solid,
    float Friction = 0.8f,
    FluidProperties? Fluid = null,
    ResourceLocation? TextureTop = null,
    ResourceLocation? TextureBottom = null,
    ResourceLocation? TextureSides = null
);