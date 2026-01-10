namespace SharpCraft.Core.WorldGeneration;

/// <summary>
/// Defines an interface for a world generator.
/// </summary>
public interface IWorldGenerator
{
    /// <summary>
    /// Generates blocks for a given chunk.
    /// </summary>
    /// <param name="chunk">The chunk to populate.</param>
    public void GenerateChunk(Chunk chunk);
}