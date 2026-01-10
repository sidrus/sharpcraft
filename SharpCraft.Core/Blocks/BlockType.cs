namespace SharpCraft.Core.Blocks;

/// <summary>
/// Defines the available block types in the game.
/// </summary>
public enum BlockType : byte
{
    /// <summary>
    /// Represents empty space.
    /// </summary>
    Air = 0,

    /// <summary>
    /// A basic dirt block.
    /// </summary>
    Dirt = 1,

    /// <summary>
    /// A dirt block with a grass top.
    /// </summary>
    Grass = 2,

    /// <summary>
    /// A solid stone block.
    /// </summary>
    Stone = 3,

    /// <summary>
    /// Sand block.
    /// </summary>
    Sand = 4,

    /// <summary>
    /// Water block.
    /// </summary>
    Water = 5,

    /// <summary>
    /// Lava block.
    /// </summary>
    Lava = 6,

    /// <summary>
    /// Unbreakable bedrock.
    /// </summary>
    Bedrock = 7
}