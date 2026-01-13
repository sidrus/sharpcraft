using SharpCraft.Core.Blocks;
using SharpCraft.Core.WorldGeneration.Noise;

namespace SharpCraft.Core.WorldGeneration;

/// <summary>
/// Implements the default procedural world generation.
/// </summary>
public class DefaultWorldGenerator(int seed = 12345) : IWorldGenerator
{
    private readonly INoiseGenerator _continentNoise = new SimplexNoise(seed);
    private readonly INoiseGenerator _terrainNoise = new SimplexNoise(seed);
    private readonly INoiseGenerator _detailNoise = new SimplexNoise(seed);

    /// <inheritdoc />
    public void GenerateChunk(Chunk chunk)
    {
        for (var x = 0; x < Chunk.Size; x++)
        {
            for (var z = 0; z < Chunk.Size; z++)
            {
                var worldX = (int)chunk.WorldPosition.X + x;
                var worldZ = (int)chunk.WorldPosition.Z + z;
                var height = GetTerrainHeight(worldX, worldZ);
                for (var y = 0; y < Chunk.Height; y++)
                {
                    var type = GetBlockType(worldX, y, worldZ, height);
                    chunk.SetBlock(x, y, z, type);
                }
            }
        }
    }

    private int GetTerrainHeight(int x, int z)
    {
        var continent = _continentNoise.Evaluate(x * 0.0005f, z * 0.0005f) * 40f;
        var terrain = _terrainNoise.Evaluate(x * 0.01f, z * 0.01f) * 20f;
        var detail = _detailNoise.Evaluate(x * 0.001f, z * 0.001f) * 5f;

        const int baseHeight = 64;
        return baseHeight + (int)(continent + terrain + detail);
    }

    private static BlockType GetBlockType(int x, int y, int z, int surfaceHeight)
    {
        if (y > surfaceHeight)
        {
            return y <= 62 ? BlockType.Water : BlockType.Air;
        }

        if (y == surfaceHeight)
        {
            return y < 62 ? BlockType.Sand : BlockType.Grass;
        }

        return y >= surfaceHeight - 3 ? BlockType.Dirt : BlockType.Stone;
    }
}