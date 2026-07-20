namespace SharpCraft.Sdk.Blocks;

/// <summary>
/// Per-voxel physical traits cached on <see cref="Block"/> so the mesh and physics hot loops
/// need no registry lookup. Derived from the block's <see cref="BlockDefinition"/> when placed.
/// </summary>
[Flags]
public enum BlockFlags : byte
{
    /// <summary>No traits (also the value for empty space / air).</summary>
    None = 0,

    /// <summary>Blocks movement and forms collidable geometry.</summary>
    Solid = 1,

    /// <summary>Renders faces against it (glass, water) instead of culling them.</summary>
    Transparent = 2,

    /// <summary>Is a fluid the player swims through.</summary>
    Fluid = 4
}
