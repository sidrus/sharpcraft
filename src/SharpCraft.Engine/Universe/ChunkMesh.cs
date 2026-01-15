namespace SharpCraft.Engine.Universe;

/// <summary>
/// Contains vertex and index data for a chunk's geometry.
/// </summary>
public record ChunkMesh
{
    /// <summary>
    /// Gets or sets the vertex data.
    /// </summary>
    public float[] Vertices { get; init; } = [];

    /// <summary>
    /// Gets or sets the index data.
    /// </summary>
    public uint[] Indices { get; init; } = [];
}
