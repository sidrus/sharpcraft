using System.Numerics;
using SharpCraft.Core.Blocks;
using SharpCraft.Core.Numerics;

namespace SharpCraft.Core.WorldGeneration;

public class Chunk(Vector2<int> coord)
{
    public const int Size = 16;
    public const int Height = 256;

    public ChunkMesh OpaqueMesh { get; private set; }
    public ChunkMesh TransparentMesh { get; private set; }

    public float[] Vertices => OpaqueMesh.Vertices;
    public uint[] Indices => OpaqueMesh.Indices;

    public Vector3 WorldPosition { get; } = new(coord.X * Size, 0, coord.Y * Size);
    public bool IsDirty { get; private set; } = true;

    private readonly Block[,,] _blocks = new Block[Size, Height, Size];
    private readonly Lock _lockObject = new();

    public Block GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Height || z < 0 || z >= Size)
        {
            return new Block { Type = BlockType.Air };
        }

        return _blocks[x, y, z];
    }

    public void SetBlock(int x, int y, int z, BlockType type)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Height || z < 0 || z >= Size)
        {
            return;
        }

        _blocks[x, y, z].Type = type;
        IsDirty = true;
    }

    public void GenerateMesh(World world)
    {
        if (!IsDirty) return;

        // Pre-allocate some capacity to reduce reallocations.
        // Average chunk might have ~2000-4000 vertices? Let's start with a reasonable guess.
        var opaqueVerts = new List<float>(4096);
        var opaqueIndices = new List<uint>(1024);
        uint opaqueIndexOffset = 0;

        var transparentVerts = new List<float>(1024);
        var transparentIndices = new List<uint>(256);
        uint transparentIndexOffset = 0;

        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                for (var z = 0; z < Size; z++)
                {
                    var block = _blocks[x, y, z];
                    if (block.Type == BlockType.Air) continue;

                    var isTransparent = block.IsTransparent;
                    var vList = isTransparent ? transparentVerts : opaqueVerts;
                    var iList = isTransparent ? transparentIndices : opaqueIndices;
                    ref var offset = ref (isTransparent ? ref transparentIndexOffset : ref opaqueIndexOffset);

                    if (ShouldRenderFace(world, x, y, z - 1, isTransparent)) // North (-Z)
                        AddFace(vList, iList, ref offset, x, y, z, Direction.North, block.Type);

                    if (ShouldRenderFace(world, x, y, z + 1, isTransparent)) // South (+Z)
                        AddFace(vList, iList, ref offset, x, y, z, Direction.South, block.Type);

                    if (ShouldRenderFace(world, x + 1, y, z, isTransparent)) // East (+X)
                        AddFace(vList, iList, ref offset, x, y, z, Direction.East, block.Type);

                    if (ShouldRenderFace(world, x - 1, y, z, isTransparent)) // West (-X)
                        AddFace(vList, iList, ref offset, x, y, z, Direction.West, block.Type);

                    if (ShouldRenderFace(world, x, y + 1, z, isTransparent)) // Up (+Y)
                        AddFace(vList, iList, ref offset, x, y, z, Direction.Up, block.Type);

                    if (ShouldRenderFace(world, x, y - 1, z, isTransparent)) // Down (-Y)
                        AddFace(vList, iList, ref offset, x, y, z, Direction.Down, block.Type);
                }
            }
        }

        var newOpaqueMesh = new ChunkMesh { Vertices = opaqueVerts.ToArray(), Indices = opaqueIndices.ToArray() };
        var newTransparentMesh = new ChunkMesh
            { Vertices = transparentVerts.ToArray(), Indices = transparentIndices.ToArray() };

        lock (_lockObject)
        {
            OpaqueMesh = newOpaqueMesh;
            TransparentMesh = newTransparentMesh;
            IsDirty = false;
        }
    }

    private bool ShouldRenderFace(World world, int x, int y, int z, bool currentIsTransparent = false)
    {
        // 1. Check within chunk bounds first
        if (x is >= 0 and < Size && y is >= 0 and < Height && z is >= 0 and < Size)
        {
            var neighbor = _blocks[x, y, z];
            if (neighbor.Type == BlockType.Air) return true;

            // If we are water, don't render against water
            if (currentIsTransparent) return !neighbor.IsTransparent;

            // If we are solid (e.g. Sand), don't render against Water to prevent Z-fighting
            // Only render solid faces against Air or non-water transparency (like Glass)
            return neighbor.IsTransparent && neighbor.Type != BlockType.Water;
        }

        // 2. Check neighboring chunk
        var worldX = (int)WorldPosition.X + x;
        var worldZ = (int)WorldPosition.Z + z;

        if (y is < 0 or >= Height) return true;

        var neighborBlock = world.GetBlock(worldX, y, worldZ);
        if (neighborBlock.Type == BlockType.Air) return true;
        if (currentIsTransparent) return !neighborBlock.IsTransparent;
        return neighborBlock.IsTransparent && neighborBlock.Type != BlockType.Water;
    }

    private static void AddFace(List<float> vertices, List<uint> indices, ref uint offset,
        int x, int y, int z, Direction dir, BlockType type)
    {
        // Two triangles per face
        indices.Add(offset);
        indices.Add(offset + 1);
        indices.Add(offset + 2);

        indices.Add(offset);
        indices.Add(offset + 2);
        indices.Add(offset + 3);

        offset += 4;

        // Position data
        float fx = x, fy = y, fz = z;
        Span<float> faceVerts = stackalloc float[12];
        switch (dir)
        {
            case Direction.Up:
                faceVerts[0] = fx; faceVerts[1] = fy + 1; faceVerts[2] = fz;
                faceVerts[3] = fx; faceVerts[4] = fy + 1; faceVerts[5] = fz + 1;
                faceVerts[6] = fx + 1; faceVerts[7] = fy + 1; faceVerts[8] = fz + 1;
                faceVerts[9] = fx + 1; faceVerts[10] = fy + 1; faceVerts[11] = fz;
                break;
            case Direction.Down:
                faceVerts[0] = fx; faceVerts[1] = fy; faceVerts[2] = fz;
                faceVerts[3] = fx + 1; faceVerts[4] = fy; faceVerts[5] = fz;
                faceVerts[6] = fx + 1; faceVerts[7] = fy; faceVerts[8] = fz + 1;
                faceVerts[9] = fx; faceVerts[10] = fy; faceVerts[11] = fz + 1;
                break;
            case Direction.North:
                faceVerts[0] = fx + 1; faceVerts[1] = fy; faceVerts[2] = fz;
                faceVerts[3] = fx; faceVerts[4] = fy; faceVerts[5] = fz;
                faceVerts[6] = fx; faceVerts[7] = fy + 1; faceVerts[8] = fz;
                faceVerts[9] = fx + 1; faceVerts[10] = fy + 1; faceVerts[11] = fz;
                break;
            case Direction.South:
                faceVerts[0] = fx; faceVerts[1] = fy; faceVerts[2] = fz + 1;
                faceVerts[3] = fx + 1; faceVerts[4] = fy; faceVerts[5] = fz + 1;
                faceVerts[6] = fx + 1; faceVerts[7] = fy + 1; faceVerts[8] = fz + 1;
                faceVerts[9] = fx; faceVerts[10] = fy + 1; faceVerts[11] = fz + 1;
                break;
            case Direction.East:
                faceVerts[0] = fx + 1; faceVerts[1] = fy; faceVerts[2] = fz + 1;
                faceVerts[3] = fx + 1; faceVerts[4] = fy; faceVerts[5] = fz;
                faceVerts[6] = fx + 1; faceVerts[7] = fy + 1; faceVerts[8] = fz;
                faceVerts[9] = fx + 1; faceVerts[10] = fy + 1; faceVerts[11] = fz + 1;
                break;
            case Direction.West:
                faceVerts[0] = fx; faceVerts[1] = fy; faceVerts[2] = fz;
                faceVerts[3] = fx; faceVerts[4] = fy; faceVerts[5] = fz + 1;
                faceVerts[6] = fx; faceVerts[7] = fy + 1; faceVerts[8] = fz + 1;
                faceVerts[9] = fx; faceVerts[10] = fy + 1; faceVerts[11] = fz;
                break;
        }

        // Texture data
        var tileIndex = type switch
        {
            BlockType.Grass => dir switch
            {
                Direction.Up => 0,
                Direction.Down => 2,
                _ => 3
            },
            BlockType.Dirt => 2,
            BlockType.Stone => 1,
            BlockType.Sand => 18,
            BlockType.Water => 19,
            BlockType.Bedrock => 17,
            _ => 0
        };

        const float atlasSize = 16f;
        const float tileSize = 1f / atlasSize;
        var tx = (tileIndex % 16) * tileSize;
        var ty = (tileIndex / 16) * tileSize;

        Span<float> uvs = stackalloc float[8];
        uvs[0] = tx; uvs[1] = ty + tileSize;
        uvs[2] = tx + tileSize; uvs[3] = ty + tileSize;
        uvs[4] = tx + tileSize; uvs[5] = ty;
        uvs[6] = tx; uvs[7] = ty;

        // Normals
        float nx = 0, ny = 0, nz = 0;
        switch (dir)
        {
            case Direction.Up: ny = 1f; break;
            case Direction.Down: ny = -1f; break;
            case Direction.North: nz = -1f; break;
            case Direction.South: nz = 1f; break;
            case Direction.East: nx = 1f; break;
            case Direction.West: nx = -1f; break;
        }

        for (var i = 0; i < 4; i++)
        {
            vertices.Add(faceVerts[i * 3]);
            vertices.Add(faceVerts[i * 3 + 1]);
            vertices.Add(faceVerts[i * 3 + 2]);
            vertices.Add(uvs[i * 2]);
            vertices.Add(uvs[i * 2 + 1]);
            vertices.Add(nx);
            vertices.Add(ny);
            vertices.Add(nz);
        }
    }
}