using SharpCraft.Sdk.Resources;

namespace SharpCraft.Sdk.Blocks;

/// <summary>
/// Registry of block definitions that also assigns each a compact numeric id for per-voxel storage.
/// </summary>
public interface IBlockRegistry : IRegistry<BlockDefinition>
{
    /// <summary>Gets the numeric id for a block location, or 0 if unregistered.</summary>
    ushort GetId(ResourceLocation id);

    /// <summary>Gets the definition for a numeric id, or null if none.</summary>
    BlockDefinition? GetById(ushort numericId);
}