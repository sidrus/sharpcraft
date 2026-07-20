using SharpCraft.Sdk.Blocks;

namespace SharpCraft.Sdk.Universe;

/// <summary>
/// Represents the game world for rendering and collision purposes.
/// </summary>
public interface IWorld
{
    /// <summary>
    /// Gets the current size of the world (render distance).
    /// </summary>
    int Size
    {
        get;
    }

    /// <summary>
    /// Gets the block at the specified world coordinates.
    /// </summary>
    Block GetBlock(int worldX, int worldY, int worldZ);

    /// <summary>
    /// Gets all currently loaded chunks.
    /// </summary>
    IEnumerable<IChunk> GetLoadedChunks();
}