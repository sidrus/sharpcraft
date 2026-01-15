using System.Collections.Concurrent;
using System.Numerics;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Numerics;
using SharpCraft.Sdk.Physics;

namespace SharpCraft.Engine.Universe;

/// <summary>
/// Represents the game world, containing all chunks and entities.
/// </summary>
public class World(int seed = 12345) : ICollisionProvider
{
    private readonly ConcurrentDictionary<Vector2<int>, Chunk> _chunks = new();
    private readonly IWorldGenerator _generator = new DefaultWorldGenerator(seed);

    /// <summary>
    /// Gets the current size of the world (render distance).
    /// </summary>
    public int Size { get; private set; }

    /// <summary>
    /// Gets the horizontal size of a chunk.
    /// </summary>
    public static int ChunkSize => Chunk.Size;

    /// <summary>
    /// Generates the world asynchronously around a center point.
    /// </summary>
    /// <param name="bounds">The number of chunks to generate in each direction from the center.</param>
    /// <param name="center">The center point in world space. Defaults to Zero.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task GenerateAsync(int bounds, Vector3? center = null)
    {
        Size = bounds;
        
        var currentCenter = center ?? Vector3.Zero;
        var coords = GetCoordsInRange(currentCenter, bounds);

        // Process in batches to balance performance.
        const int batchSize = 16;
        for (var i = 0; i < coords.Count; i += batchSize)
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

    /// <summary>
    /// Unloads chunks that are outside the specified range from the center.
    /// </summary>
    /// <param name="center">The center point.</param>
    /// <param name="range">The range in chunks.</param>
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

    /// <summary>
    /// Generates chunks within the specified bounds from the origin.
    /// </summary>
    /// <param name="bounds">The number of chunks in each direction.</param>
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

    /// <summary>
    /// Gets an existing chunk or creates a new one at the specified coordinates.
    /// </summary>
    /// <param name="coord">The chunk coordinates.</param>
    /// <returns>The chunk.</returns>
    public Chunk GetOrCreateChunk(Vector2<int> coord)
    {
        return _chunks.GetOrAdd(coord, k =>
        {
            var chunk = new Chunk(k);
            _generator.GenerateChunk(chunk);
            return chunk;
        });
    }

    /// <inheritdoc />
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

    /// <summary>
    /// Sets the block type at the specified world coordinates.
    /// </summary>
    /// <param name="worldX">The world X coordinate.</param>
    /// <param name="worldY">The world Y coordinate.</param>
    /// <param name="worldZ">The world Z coordinate.</param>
    /// <param name="type">The new block type.</param>
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

    /// <summary>
    /// Gets all currently loaded chunks.
    /// </summary>
    /// <returns>An enumerable of loaded chunks.</returns>
    public IEnumerable<Chunk> GetLoadedChunks() => _chunks.Values;
}
