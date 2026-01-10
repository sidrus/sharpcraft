namespace SharpCraft.Core.Blocks;

public struct Block
{
    public BlockType Type { get; set; }

    public bool IsSolid => Type != BlockType.Air &&
                           Type != BlockType.Water &&
                           Type != BlockType.Lava;
    public bool IsTransparent => Type is BlockType.Air or BlockType.Water;

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