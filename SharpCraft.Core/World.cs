using System.Numerics;
using SharpCraft.Core.Blocks;
using SharpCraft.Core.Numerics;
using SharpCraft.Core.Physics;
using SharpCraft.Core.WorldGeneration;

namespace SharpCraft.Core;

public class World(int seed = 12345) : ICollisionProvider
{
    private readonly Dictionary<Vector2<int>, Chunk> _chunks = new();
    private readonly IWorldGenerator _generator = new DefaultWorldGenerator(seed);
    public int Size { get; private set; }
    public int ChunkSize => Chunk.Size;

    public void Generate(int bounds)
    {
        Size = bounds;
        for(var x = -bounds; x <= bounds; x++)
        {
            for (var z = -bounds; z <= bounds; z++)
            {
                GetOrCreateChunk(new Vector2<int>(x, z));
            }
        }
    }

    public Chunk GetOrCreateChunk(Vector2<int> coord)
    {
        if (_chunks.TryGetValue(coord, out var chunk))
        {
            return chunk;
        }

        chunk = new Chunk(coord);
        _generator.GenerateChunk(chunk);
        _chunks[coord] = chunk;

        return chunk;
    }

    public Block GetBlock(int worldX, int worldY, int worldZ)
    {
        if (worldY is < 0 or >= Chunk.Height)
        {
            return new Block { Type = BlockType.Air };
        }

        var chunkX = worldX >> 4;
        var chunkZ = worldZ >> 4;
        var localX = worldX & 15;
        var localZ = worldZ & 15;

        var coord = new Vector2<int>(chunkX, chunkZ);
        if (_chunks.TryGetValue(coord, out var chunk))
        {
            return chunk.GetBlock(localX, worldY, localZ);
        }

        return new Block { Type = BlockType.Air };
    }

    public void SetBlock(int worldX, int worldY, int worldZ, BlockType type)
    {
        if (worldY is < 0 or >= Chunk.Height)
        {
            return;
        }

        var chunkX = worldX >> 4;
        var chunkZ = worldZ >> 4;
        var localX = worldX & 15;
        var localZ = worldZ & 15;

        var coord = new Vector2<int>(chunkX, chunkZ);
        var chunk = GetOrCreateChunk(coord);
        chunk.SetBlock(localX, worldY, localZ, type);
    }

    public IEnumerable<Chunk> GetLoadedChunks() => _chunks.Values;
}