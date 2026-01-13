namespace SharpCraft.Core.WorldGeneration;

/// <summary>
/// Contains vertex and index data for a chunk's mesh.
/// </summary>
public struct ChunkMesh
{
    /// <summary>
    /// The vertex data.
    /// </summary>
    public float[] Vertices;

    /// <summary>
    /// The index data.
    /// </summary>
    public uint[] Indices;
}