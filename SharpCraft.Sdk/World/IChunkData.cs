namespace SharpCraft.Sdk.World;

/// <summary>
/// Provides access to chunk data during generation.
/// </summary>
public interface IChunkData
{
    /// <summary>
    /// Gets the chunk's X coordinate (in chunk units).
    /// </summary>
    int X { get; }

    /// <summary>
    /// Gets the chunk's Z coordinate (in chunk units).
    /// </summary>
    int Z { get; }

    /// <summary>
    /// Sets a block at the specified local coordinates.
    /// </summary>
    /// <param name="x">Local X [0, 15].</param>
    /// <param name="y">Local Y [0, 255].</param>
    /// <param name="z">Local Z [0, 15].</param>
    /// <param name="blockId">The namespaced block ID (e.g., "minecraft:stone").</param>
    void SetBlock(int x, int y, int z, string blockId);
}
