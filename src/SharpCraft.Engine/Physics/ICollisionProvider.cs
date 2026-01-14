using SharpCraft.Engine.Blocks;

namespace SharpCraft.Engine.Physics;

/// <summary>
/// Provides an interface for accessing blocks in the world for collision detection.
/// </summary>
public interface ICollisionProvider
{
    /// <summary>
    /// Gets the block at the specified world coordinates.
    /// </summary>
    /// <param name="worldX">The X coordinate.</param>
    /// <param name="worldY">The Y coordinate.</param>
    /// <param name="worldZ">The Z coordinate.</param>
    /// <returns>The block at the position.</returns>
    public Block GetBlock(int worldX, int worldY, int worldZ);
}
