namespace SharpCraft.Engine.Universe;

/// <summary>
/// Converts between world coordinates and chunk/local coordinates. Single source of truth for the
/// decode that <see cref="World"/> performs when reading, writing, ranging, and unloading chunks.
/// </summary>
public static class ChunkCoords
{
    /// <summary>Floor-divides a world axis coordinate to its chunk index.</summary>
    public static int ToChunk(int world) => world >> Chunk.SizeLog2;

    /// <summary>Floor-divides a world-space position to its chunk index.</summary>
    public static int ToChunk(float world) => ToChunk((int)MathF.Floor(world));

    /// <summary>Wraps a world axis coordinate to its offset within a chunk, in [0, Size).</summary>
    public static int ToLocal(int world) => world & (Chunk.Size - 1);

    /// <summary>Gets whether a world Y coordinate falls within the chunk's vertical extent.</summary>
    public static bool IsWithinHeight(int worldY) => worldY is >= 0 and < Chunk.Height;
}