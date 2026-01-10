namespace SharpCraft.Core.Blocks;

/// <summary>
/// Represents a single block in the world.
/// </summary>
public struct Block
{
    /// <summary>
    /// Gets or sets the type of the block.
    /// </summary>
    public BlockType Type { get; set; }

    /// <summary>
    /// Gets a value indicating whether the block is solid (collidable).
    /// </summary>
    public bool IsSolid => Type != BlockType.Air &&
                           Type != BlockType.Water &&
                           Type != BlockType.Lava;

    /// <summary>
    /// Gets a value indicating whether the block is transparent.
    /// </summary>
    public bool IsTransparent => Type is BlockType.Air or BlockType.Water;

    /// <summary>
    /// Gets the friction coefficient of the block.
    /// </summary>
    public float Friction => Type switch
    {
        BlockType.Grass => 0.5f,
        BlockType.Dirt => 0.5f,
        BlockType.Stone => 0.5f,
        BlockType.Air => 0.01f,
        BlockType.Water => 1f,
        _ => 0.8f
    };
}