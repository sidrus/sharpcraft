using SharpCraft.Sdk.Resources;

namespace SharpCraft.Sdk.Blocks;

/// <summary>
/// Defines the properties of a block. All traits are data, so mods add blocks without engine changes.
/// </summary>
public record BlockDefinition(
    ResourceLocation Id,
    string Name,
    bool IsSolid = true,
    bool IsTransparent = false,
    float Friction = 0.8f,
    FluidProperties? Fluid = null,
    ResourceLocation? TextureTop = null,
    ResourceLocation? TextureBottom = null,
    ResourceLocation? TextureSides = null
)
{
    /// <summary>Gets the per-voxel flags cached onto a placed <see cref="Block"/>.</summary>
    public BlockFlags Flags =>
        (IsSolid ? BlockFlags.Solid : BlockFlags.None)
        | (IsTransparent ? BlockFlags.Transparent : BlockFlags.None)
        | (Fluid is not null ? BlockFlags.Fluid : BlockFlags.None);
}
