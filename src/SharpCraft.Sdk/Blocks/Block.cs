namespace SharpCraft.Sdk.Blocks;

/// <summary>
/// A single voxel: a registry-assigned block id plus the traits cached from its definition.
/// Id 0 is empty space (air).
/// </summary>
public readonly record struct Block(ushort Id, BlockFlags Flags)
{
    /// <summary>Empty space.</summary>
    public static Block Air => default;

    /// <summary>Gets whether this voxel is empty space.</summary>
    public bool IsAir => Id == 0;

    /// <summary>Gets whether the block blocks movement.</summary>
    public bool IsSolid => (Flags & BlockFlags.Solid) != 0;

    /// <summary>Gets whether neighboring faces render against it.</summary>
    public bool IsTransparent => (Flags & BlockFlags.Transparent) != 0;

    /// <summary>Gets whether the block is a fluid.</summary>
    public bool IsFluid => (Flags & BlockFlags.Fluid) != 0;
}
