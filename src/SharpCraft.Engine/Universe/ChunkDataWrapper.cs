using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Resources;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.Engine.Universe;

/// <summary>
/// Wraps a Core Chunk to provide SDK-compatible access.
/// </summary>
public class ChunkDataWrapper(Chunk chunk) : IChunkData
{
    public int X => (int)(chunk.WorldPosition.X / Chunk.Size);
    public int Z => (int)(chunk.WorldPosition.Z / Chunk.Size);

    public void SetBlock(int x, int y, int z, ResourceLocation blockId)
    {
        chunk.SetBlock(x, y, z, blockId);
    }
}
