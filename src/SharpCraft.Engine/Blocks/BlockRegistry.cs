using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Resources;

namespace SharpCraft.Engine.Blocks;

/// <summary>
/// Runtime block registry. Assigns each registered block a sequential numeric id (from 1) so
/// voxels store a compact id; id 0 is reserved for air.
/// </summary>
public class BlockRegistry : Registry<BlockDefinition>, IBlockRegistry
{
    private readonly List<BlockDefinition?> _byId = [null];
    private readonly Dictionary<ResourceLocation, ushort> _ids = new();

    /// <inheritdoc />
    public override void Register(ResourceLocation id, BlockDefinition item)
    {
        base.Register(id, item);
        _ids[id] = (ushort)_byId.Count;
        _byId.Add(item);
    }

    /// <inheritdoc />
    public ushort GetId(ResourceLocation id)
    {
        return _ids.TryGetValue(id, out var numericId) ? numericId : (ushort)0;
    }

    /// <inheritdoc />
    public BlockDefinition? GetById(ushort numericId)
    {
        return numericId < _byId.Count ? _byId[numericId] : null;
    }
}
