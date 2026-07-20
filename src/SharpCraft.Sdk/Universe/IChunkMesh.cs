namespace SharpCraft.Sdk.Universe;

/// <summary>
/// Represents mesh data for a chunk.
/// </summary>
public interface IChunkMesh
{
    /// <summary>
    /// Gets the vertex data.
    /// </summary>
    float[] Vertices { get; }

    /// <summary>
    /// Gets the index data.
    /// </summary>
    uint[] Indices { get; }
}