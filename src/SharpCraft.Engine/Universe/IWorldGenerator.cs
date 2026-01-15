namespace SharpCraft.Engine.Universe;

/// <summary>
/// Defines a provider for world generation logic.
/// </summary>
public interface IWorldGenerator
{
    /// <summary>
    /// Generates blocks for the specified chunk.
    /// </summary>
    /// <param name="chunk">The chunk to populate.</param>
    void GenerateChunk(Chunk chunk);
}
