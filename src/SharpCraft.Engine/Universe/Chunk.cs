using SharpCraft.Sdk;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Numerics;
using SharpCraft.Sdk.Resources;
using SharpCraft.Sdk.Universe;
using System.Numerics;

namespace SharpCraft.Engine.Universe;

/// <summary>
/// Represents a 16x256x16 section of the world.
/// </summary>
public class Chunk(Vector2<int> coord, IBlockRegistry blockRegistry) : IChunkData, IChunk
{
    public int X => coord.X;

    public int Z => coord.Y;
    /// <summary>
    /// The horizontal size of a chunk in blocks.
    /// </summary>
    public const int Size = 16;

    /// <summary>
    /// Base-2 logarithm of <see cref="Size"/>, so a world coordinate decomposes into chunk/local
    /// parts via shift/mask. <see cref="Size"/> must stay a power of two for this to hold.
    /// </summary>
    public const int SizeLog2 = 4;

    /// <summary>
    /// The vertical size of a chunk in blocks.
    /// </summary>
    public const int Height = 256;

    /// <summary>
    /// Gets the mesh for opaque blocks.
    /// </summary>
    public ChunkMesh OpaqueMesh { get; private set; } = new();

    /// <inheritdoc />
    IChunkMesh IChunk.OpaqueMesh => OpaqueMesh;

    /// <summary>
    /// Gets the mesh for transparent blocks.
    /// </summary>
    public ChunkMesh TransparentMesh { get; private set; } = new();

    /// <inheritdoc />
    IChunkMesh IChunk.TransparentMesh => TransparentMesh;

    /// <summary>
    /// Gets the vertices of the opaque mesh (legacy helper).
    /// </summary>
    public float[] Vertices => OpaqueMesh.Vertices;

    /// <summary>
    /// Gets the indices of the opaque mesh (legacy helper).
    /// </summary>
    public uint[] Indices => OpaqueMesh.Indices;

    /// <summary>
    /// Gets the world position of the chunk's origin.
    /// </summary>
    public Vector3 WorldPosition { get; } = new(coord.X * Size, 0, coord.Y * Size);

    /// <summary>
    /// Gets a value indicating whether the chunk needs to be re-meshed.
    /// </summary>
    public bool IsDirty => Volatile.Read(ref _isDirtyBacking) == 1;

    /// <summary>
    /// Flags the chunk for re-meshing. Needed when a neighboring chunk is loaded or unloaded: this
    /// chunk's boundary faces depend on the neighbor's blocks (see ShouldRenderFace's cross-chunk
    /// path), so a neighbor appearing/disappearing must trigger a re-mesh or the border faces go
    /// stale (culled against blocks that no longer exist → see-through holes, or vice-versa).
    /// </summary>
    public void MarkDirty() => Volatile.Write(ref _isDirtyBacking, 1);

    private int _isDirtyBacking = 1; // 1 = true, 0 = false
    private readonly Block[,,] _blocks = new Block[Size, Height, Size];
    private readonly ReaderWriterLockSlim _blockLock = new(LockRecursionPolicy.NoRecursion);
    private readonly Lock _meshLock = new();

    /// <summary>
    /// Gets the block at the specified local coordinates.
    /// </summary>
    /// <param name="x">Local X [0, Size-1].</param>
    /// <param name="y">Local Y [0, Height-1].</param>
    /// <param name="z">Local Z [0, Size-1].</param>
    /// <returns>The block at the position, or Air if out of bounds.</returns>
    public Block GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Height || z < 0 || z >= Size)
        {
            return Block.Air;
        }

        _blockLock.EnterReadLock();
        try
        {
            return _blocks[x, y, z];
        }
        finally
        {
            _blockLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Sets the block at the specified local coordinates, resolving its id and flags from the registry.
    /// </summary>
    /// <param name="x">Local X [0, Size-1].</param>
    /// <param name="y">Local Y [0, Height-1].</param>
    /// <param name="z">Local Z [0, Size-1].</param>
    /// <param name="blockId">The block's resource location.</param>
    public void SetBlock(int x, int y, int z, ResourceLocation blockId)
    {
        var id = blockRegistry.GetId(blockId);
        var flags = blockRegistry.GetById(id)?.Flags ?? BlockFlags.None;
        SetBlock(x, y, z, new Block(id, flags));
    }

    private void SetBlock(int x, int y, int z, Block block)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Height || z < 0 || z >= Size)
        {
            return;
        }

        _blockLock.EnterWriteLock();
        try
        {
            _blocks[x, y, z] = block;
            Volatile.Write(ref _isDirtyBacking, 1);
        }
        finally
        {
            _blockLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// A delegate that resolves UV coordinates for a block id and face direction.
    /// </summary>
    public delegate void UvResolver(ushort blockId, Direction dir, Span<float> uvs);

    /// <inheritdoc />
    void IChunk.GenerateMesh(IWorld world, IChunk.UvResolver uvResolver)
    {
        GenerateMesh(world, (blockId, dir, uvs) => uvResolver(blockId, dir, uvs));
    }

    /// <summary>
    /// Generates the visual meshes for the chunk.
    /// </summary>
    /// <param name="world">The world context for neighbor checks.</param>
    /// <param name="uvResolver">The UV resolver.</param>
    public void GenerateMesh(IWorld world, UvResolver uvResolver)
    {
        if (!IsDirty)
        {
            return;
        }

        // Create snapshot of block data with read lock
        Block[,,] blockSnapshot = new Block[Size, Height, Size];
        _blockLock.EnterReadLock();
        try
        {
            Array.Copy(_blocks, blockSnapshot, _blocks.Length);
        }
        finally
        {
            _blockLock.ExitReadLock();
        }

        var (newOpaqueMesh, newTransparentMesh) = ChunkMesher.Generate(blockSnapshot, WorldPosition, world, uvResolver);

        lock (_meshLock)
        {
            OpaqueMesh = newOpaqueMesh;
            TransparentMesh = newTransparentMesh;
            Volatile.Write(ref _isDirtyBacking, 0);
        }
    }

    /// <summary>
    /// Disposes the chunk, releasing lock resources.
    /// </summary>
    public void Dispose()
    {
        _blockLock.Dispose();
    }
}