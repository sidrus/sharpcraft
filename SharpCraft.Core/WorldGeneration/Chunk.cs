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

        var opaqueVerts = new List<float>();
        var opaqueIndices = new List<uint>();
        uint opaqueIndexOffset = 0;

        var transparentVerts = new List<float>();
        var transparentIndices = new List<uint>();
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

                    var blockPos = new Vector3(x, y, z);

                    if (ShouldRenderFace(world, x, y, z - 1, isTransparent)) // North (-Z)
                        AddFace(vList, iList, ref offset, blockPos, Direction.North, block.Type);

                    if (ShouldRenderFace(world, x, y, z + 1, isTransparent)) // South (+Z)
                        AddFace(vList, iList, ref offset, blockPos, Direction.South, block.Type);

                    if (ShouldRenderFace(world, x + 1, y, z, isTransparent)) // East (+X)
                        AddFace(vList, iList, ref offset, blockPos, Direction.East, block.Type);

                    if (ShouldRenderFace(world, x - 1, y, z, isTransparent)) // West (-X)
                        AddFace(vList, iList, ref offset, blockPos, Direction.West, block.Type);

                    if (ShouldRenderFace(world, x, y + 1, z, isTransparent)) // Up (+Y)
                        AddFace(vList, iList, ref offset, blockPos, Direction.Up, block.Type);

                    if (ShouldRenderFace(world, x, y - 1, z, isTransparent)) // Down (-Y)
                        AddFace(vList, iList, ref offset, blockPos, Direction.Down, block.Type);
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
        Vector3 pos, Direction dir, BlockType type)
    {
        var faceVerts = GetFaceVertices(pos, dir);
        var uvs = GetTextureCoords(type, dir);
        var normals = GetNormal(dir);

        // Interleave: position(3) + uv(2) + normal(3)
        for (var i = 0; i < 4; i++)
        {
            vertices.Add(faceVerts[i * 3]); // x
            vertices.Add(faceVerts[i * 3 + 1]); // y
            vertices.Add(faceVerts[i * 3 + 2]); // z
            vertices.Add(uvs[i * 2]); // u
            vertices.Add(uvs[i * 2 + 1]); // v
            vertices.Add(normals[0]); // nx
            vertices.Add(normals[1]); // ny
            vertices.Add(normals[2]); // nz
        }

        // Two triangles per face
        indices.Add(offset);
        indices.Add(offset + 1);
        indices.Add(offset + 2);

        indices.Add(offset);
        indices.Add(offset + 2);
        indices.Add(offset + 3);

        offset += 4;
    }

    private static float[] GetFaceVertices(Vector3 pos, Direction dir)
    {
        float x = pos.X, y = pos.Y, z = pos.Z;

        return dir switch
        {
            // Up: +Y face
            Direction.Up =>
            [
                x, y + 1, z,
                x, y + 1, z + 1,
                x + 1, y + 1, z + 1,
                x + 1, y + 1, z
            ],

            // Down: -Y face
            Direction.Down =>
            [
                x, y, z,
                x + 1, y, z,
                x + 1, y, z + 1,
                x, y, z + 1
            ],

            // North: -Z face
            Direction.North =>
            [
                x + 1, y, z,
                x, y, z,
                x, y + 1, z,
                x + 1, y + 1, z
            ],

            // South: +Z face
            Direction.South =>
            [
                x, y, z + 1,
                x + 1, y, z + 1,
                x + 1, y + 1, z + 1,
                x, y + 1, z + 1
            ],

            // East: +X face
            Direction.East =>
            [
                x + 1, y, z + 1,
                x + 1, y, z,
                x + 1, y + 1, z,
                x + 1, y + 1, z + 1
            ],

            // West: -X face
            Direction.West =>
            [
                x, y, z,
                x, y, z + 1,
                x, y + 1, z + 1,
                x, y + 1, z
            ],
            _ => new float[12]
        };
    }

    private static float[] GetTextureCoords(BlockType type, Direction dir)
    {
        // Define tile indices (row * 16 + col)
        var tileIndex = type switch
        {
            BlockType.Grass => dir switch
            {
                Direction.Up => 0,
                Direction.Down => 2,
                _ => 3
            }, // Top, Bottom, Side

            BlockType.Dirt => 2,
            BlockType.Stone => 1,
            BlockType.Sand => 18,
            BlockType.Water => 19,
            BlockType.Bedrock => 17,
            _ => 0
        };

        const float atlasSize = 16f; // 16x16 tiles
        const float tileSize = 1f / atlasSize;

        var x = tileIndex % 16;
        var y = tileIndex / 16;

        var u = x * tileSize;
        var v = y * tileSize;

        // Return 4 pairs of UVs for the face corners
        return
        [
            u, v + tileSize, // Bottom Left
            u + tileSize, v + tileSize, // Bottom Right
            u + tileSize, v, // Top Right
            u, v // Top Left
        ];
    }

    private static float[] GetNormal(Direction dir)
    {
        return dir switch
        {
            Direction.Up => [0f, 1f, 0f],
            Direction.Down => [0f, -1f, 0f],
            Direction.North => [0f, 0f, -1f],
            Direction.South => [0f, 0f, 1f],
            Direction.East => [1f, 0f, 0f],
            Direction.West => [-1f, 0f, 0f],
            _ => [0f, 0f, 0f]
        };
    }
}