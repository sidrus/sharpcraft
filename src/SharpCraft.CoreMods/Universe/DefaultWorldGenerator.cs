using SharpCraft.CoreMods.Universe.Noise;
using SharpCraft.Sdk.Resources;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.CoreMods.Universe;

/// <summary>
/// Implements the default procedural world generation.
/// </summary>
public class DefaultWorldGenerator : IWorldGenerator
{
    private static readonly ResourceLocation Grass = new("sharpcraft", "grass");
    private static readonly ResourceLocation Dirt = new("sharpcraft", "dirt");
    private static readonly ResourceLocation Stone = new("sharpcraft", "stone");
    private static readonly ResourceLocation Sand = new("sharpcraft", "sand");
    private static readonly ResourceLocation Water = new("sharpcraft", "water");
    private static readonly ResourceLocation Air = new("sharpcraft", "air");

    private INoiseGenerator? _continentNoise;
    private INoiseGenerator? _terrainNoise;
    private INoiseGenerator? _detailNoise;
    private long _currentSeed = -1;

    /// <inheritdoc />
    public void GenerateChunk(IChunkData chunk, long seed)
    {
        InitializeNoise(seed);

        for (var x = 0; x < 16; x++)
        {
            for (var z = 0; z < 16; z++)
            {
                var worldX = (chunk.X * 16) + x;
                var worldZ = (chunk.Z * 16) + z;
                var height = GetTerrainHeight(worldX, worldZ);
                for (var y = 0; y < 256; y++)
                {
                    var type = GetBlockType(worldX, y, worldZ, height);
                    chunk.SetBlock(x, y, z, type);
                }
            }
        }
    }

    private void InitializeNoise(long seed)
    {
        if (_currentSeed == seed && _continentNoise != null) return;

        var s = (int)seed;
        _continentNoise = new SimplexNoise(s);
        _terrainNoise = new SimplexNoise(s);
        _detailNoise = new SimplexNoise(s);
        _currentSeed = seed;
    }

    private int GetTerrainHeight(int x, int z)
    {
        var continent = _continentNoise!.Evaluate(x * 0.0005f, z * 0.0005f) * 40f;
        var terrain = _terrainNoise!.Evaluate(x * 0.01f, z * 0.01f) * 20f;
        var detail = _detailNoise!.Evaluate(x * 0.001f, z * 0.001f) * 5f;

        const int baseHeight = 64;
        return baseHeight + (int)(continent + terrain + detail);
    }

    private static ResourceLocation GetBlockType(int x, int y, int z, int surfaceHeight)
    {
        if (y > surfaceHeight)
        {
            return y <= 62 ? Water : Air;
        }

        if (y == surfaceHeight)
        {
            return y < 62 ? Sand : Grass;
        }

        return y >= surfaceHeight - 3 ? Dirt : Stone;
    }
}
