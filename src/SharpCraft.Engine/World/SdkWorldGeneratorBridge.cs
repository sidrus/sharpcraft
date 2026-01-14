using SharpCraft.Core.WorldGeneration;
using SharpCraft.Sdk.World;

namespace SharpCraft.Engine.World;

/// <summary>
/// Bridges an SDK IWorldGenerator to the Core engine's IWorldGenerator.
/// </summary>
public class SdkWorldGeneratorBridge(SharpCraft.Sdk.World.IWorldGenerator sdkGenerator, long seed)
    : SharpCraft.Core.WorldGeneration.IWorldGenerator
{
    public void GenerateChunk(Chunk chunk)
    {
        var wrapper = new ChunkDataWrapper(chunk);
        sdkGenerator.GenerateChunk(wrapper, seed);
    }
}
