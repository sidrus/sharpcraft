using SharpCraft.Engine.Blocks;
using SharpCraft.Sdk.World;

namespace SharpCraft.Engine.World;

/// <summary>
/// Wraps a Core Chunk to provide SDK-compatible access.
/// </summary>
public class ChunkDataWrapper(Chunk chunk) : IChunkData
{
    public int X => (int)(chunk.WorldPosition.X / Chunk.Size);
    public int Z => (int)(chunk.WorldPosition.Z / Chunk.Size);

    public void SetBlock(int x, int y, int z, string blockId)
    {
        // Simple mapping for Phase 1. 
        // In a full implementation, this would use a registry-based lookup.
        var type = blockId.ToLower() switch
        {
            "sharpcraft:air" => BlockType.Air,
            "sharpcraft:dirt" => BlockType.Dirt,
            "sharpcraft:grass" => BlockType.Grass,
            "sharpcraft:stone" => BlockType.Stone,
            "sharpcraft:sand" => BlockType.Sand,
            "sharpcraft:water" => BlockType.Water,
            "sharpcraft:lava" => BlockType.Lava,
            "sharpcraft:bedrock" => BlockType.Bedrock,
            // Fallback for common aliases
            "air" => BlockType.Air,
            "dirt" => BlockType.Dirt,
            "grass" => BlockType.Grass,
            "stone" => BlockType.Stone,
            _ => BlockType.Air 
        };

        chunk.SetBlock(x, y, z, type);
    }
}
