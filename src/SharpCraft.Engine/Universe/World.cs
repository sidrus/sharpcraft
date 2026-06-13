using System.Collections.Concurrent;
using System.Numerics;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Numerics;
using SharpCraft.Sdk.Physics;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.Engine.Universe;

/// <summary>
/// Represents the game world, containing all chunks and entities.
/// </summary>
public class World(IWorldGenerator generator, long seed, IBlockRegistry blockRegistry) : ICollisionProvider, IWorld
{
    private readonly ConcurrentDictionary<Vector2<int>, Chunk> _chunks = new();

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
            if (_chunks.TryRemove(coord, out var chunk))
            {
                chunk.Dispose();
                // The removed chunk's neighbours culled their boundary faces against it; with it gone
                // those faces are now exposed, so the neighbours must re-mesh or they leave see-through
                // holes at the unload boundary.
                MarkNeighborsDirty(coord);
            }
        }
    }

    /// <summary>
    /// Flags the four horizontal neighbours of a chunk coordinate for re-meshing, so their
    /// cross-chunk boundary faces are recomputed when this chunk is loaded or unloaded.
    /// </summary>
    private void MarkNeighborsDirty(Vector2<int> coord)
    {
        Span<Vector2<int>> neighbors =
        [
            new(coord.X + 1, coord.Y),
            new(coord.X - 1, coord.Y),
            new(coord.X, coord.Y + 1),
            new(coord.X, coord.Y - 1)
        ];

        foreach (var n in neighbors)
        {
            if (_chunks.TryGetValue(n, out var neighbor))
            {
                neighbor.MarkDirty();
            }
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
        var created = false;
        var chunk = _chunks.GetOrAdd(coord, k =>
        {
            created = true;
            var c = new Chunk(k, blockRegistry);
            generator.GenerateChunk(c, seed);
            return c;
        });

        if (created)
        {
            // A newly loaded chunk changes its neighbours' boundary faces (previously exposed to the
            // void, now culled against this chunk's blocks — or the reverse), so re-mesh them.
            MarkNeighborsDirty(coord);
        }

        return chunk;
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

        // If the edited block sits on a chunk edge, the adjacent chunk's boundary faces depend on it
        // (ShouldRenderFace's cross-chunk path), so re-mesh that neighbour too. A corner edit touches
        // two edges and dirties both.
        if (localX == 0) MarkDirtyIfLoaded(new Vector2<int>(chunkX - 1, chunkZ));
        else if (localX == 15) MarkDirtyIfLoaded(new Vector2<int>(chunkX + 1, chunkZ));
        if (localZ == 0) MarkDirtyIfLoaded(new Vector2<int>(chunkX, chunkZ - 1));
        else if (localZ == 15) MarkDirtyIfLoaded(new Vector2<int>(chunkX, chunkZ + 1));
    }

    private void MarkDirtyIfLoaded(Vector2<int> coord)
    {
        if (_chunks.TryGetValue(coord, out var chunk))
        {
            chunk.MarkDirty();
        }
    }

    /// <summary>
    /// Gets all currently loaded chunks.
    /// </summary>
    /// <returns>An enumerable of loaded chunks.</returns>
    public IEnumerable<Chunk> GetLoadedChunks() => _chunks.Values.ToArray();

    /// <inheritdoc />
    IEnumerable<IChunk> IWorld.GetLoadedChunks() => _chunks.Values.ToArray();
}
