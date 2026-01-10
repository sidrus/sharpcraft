using System.Collections.Concurrent;
using System.Numerics;
using SharpCraft.Core.Blocks;
using SharpCraft.Core.Numerics;
using SharpCraft.Core.Physics;
using SharpCraft.Core.WorldGeneration;

namespace SharpCraft.Core;

public class World(int seed = 12345) : ICollisionProvider
{
    private readonly ConcurrentDictionary<Vector2<int>, Chunk> _chunks = new();
    private readonly IWorldGenerator _generator = new DefaultWorldGenerator(seed);
    public int Size { get; private set; }
    public static int ChunkSize => Chunk.Size;

    public async Task GenerateAsync(int bounds, Vector3? center = null)
    {
        Size = bounds;
        
        var currentCenter = center ?? Vector3.Zero;
        var coords = GetCoordsInRange(currentCenter, bounds);

        // Process in batches to be more "progressive" and not overwhelm the thread pool immediately,
        // although Task.WhenAll(tasks) is still a big block.
        // To be TRULY progressive we could use Parallel.ForEachAsync with MaxDegreeOfParallelism
        const int batchSize = 16;
        for (int i = 0; i < coords.Count; i += batchSize)
        {
            var batch = coords.Skip(i).Take(batchSize);
            var tasks = batch.Select(coord => Task.Run(() => GetOrCreateChunk(coord))).ToList();
            await Task.WhenAll(tasks);
        }
    }

    private static List<Vector2<int>> GetCoordsInRange(Vector3 center, int bounds)
    {
        var centerChunkX = (int)Math.Floor(center.X / Chunk.Size);
        var centerChunkZ = (int)Math.Floor(center.Z / Chunk.Size);

        var coords = new List<Vector2<int>>();
        for (var x = centerChunkX - bounds; x <= centerChunkX + bounds; x++)
        {
            for (var z = centerChunkZ - bounds; z <= centerChunkZ + bounds; z++)
            {
                coords.Add(new Vector2<int>(x, z));
            }
        }

        coords.Sort((a, b) =>
        {
            var distA = Math.Pow(a.X - centerChunkX, 2) + Math.Pow(a.Y - centerChunkZ, 2);
            var distB = Math.Pow(b.X - centerChunkX, 2) + Math.Pow(b.Y - centerChunkZ, 2);
            return distA.CompareTo(distB);
        });

        return coords;
    }

    public void UnloadChunks(Vector3 center, int range)
    {
        var centerChunkX = (int)Math.Floor(center.X / Chunk.Size);
        var centerChunkZ = (int)Math.Floor(center.Z / Chunk.Size);

        var toRemove = _chunks.Keys.Where(coord =>
            Math.Abs(coord.X - centerChunkX) > range ||
            Math.Abs(coord.Y - centerChunkZ) > range).ToList();

        foreach (var coord in toRemove)
        {
            _chunks.TryRemove(coord, out _);
        }
    }

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
        return _chunks.GetOrAdd(coord, k =>
        {
            var chunk = new Chunk(k);
            _generator.GenerateChunk(chunk);
            return chunk;
        });
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