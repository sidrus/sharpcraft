namespace SharpCraft.Sdk.World;

/// <summary>
/// Defines a custom terrain generator.
/// </summary>
public interface IWorldGenerator
{
    /// <summary>
    /// Generates the content for a single chunk.
    /// </summary>
    /// <param name="chunk">The chunk data to populate.</param>
    /// <param name="seed">The world seed for deterministic generation.</param>
    void GenerateChunk(IChunkData chunk, long seed);
}
