

namespace SharpCraft.Engine.Universe;

/// <summary>
/// Bridges an SDK IWorldGenerator to the Core engine's IWorldGenerator.
/// </summary>
public class SdkWorldGeneratorBridge(Sdk.Universe.IWorldGenerator sdkGenerator, long seed) : IWorldGenerator
{
    public void GenerateChunk(Chunk chunk)
    {
        var wrapper = new ChunkDataWrapper(chunk);
        sdkGenerator.GenerateChunk(wrapper, seed);
    }
}
