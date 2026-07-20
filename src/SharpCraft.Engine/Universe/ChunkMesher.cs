using SharpCraft.Sdk;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Universe;
using System.Numerics;

namespace SharpCraft.Engine.Universe;

/// <summary>
/// Builds a chunk's opaque and transparent render meshes from a block snapshot. Pure geometry: it
/// owns face culling (<see cref="ShouldRenderFace"/>) and per-face vertex emission, and holds no
/// state, so it can be unit-tested without a locked <see cref="Chunk"/>.
/// </summary>
public static class ChunkMesher
{
    /// <summary>
    /// Generates the opaque and transparent meshes for a block snapshot.
    /// </summary>
    /// <param name="blocks">The chunk's block data, sized [Size, Height, Size].</param>
    /// <param name="worldPosition">The chunk origin in world space, for cross-chunk neighbor lookups.</param>
    /// <param name="world">The world context, queried for blocks in neighbouring chunks.</param>
    /// <param name="uvResolver">Resolves UV coordinates for a block id and face direction.</param>
    public static (ChunkMesh Opaque, ChunkMesh Transparent) Generate(
        Block[,,] blocks, Vector3 worldPosition, IWorld world, Chunk.UvResolver uvResolver)
    {
        var opaqueVerts = new List<float>(4096);
        var opaqueIndices = new List<uint>(1024);
        uint opaqueIndexOffset = 0;

        var transparentVerts = new List<float>(1024);
        var transparentIndices = new List<uint>(256);
        uint transparentIndexOffset = 0;

        for (var x = 0; x < Chunk.Size; x++)
        {
            for (var y = 0; y < Chunk.Height; y++)
            {
                for (var z = 0; z < Chunk.Size; z++)
                {
                    var block = blocks[x, y, z];
                    if (block.IsAir)
                    {
                        continue;
                    }

                    var isTransparent = block.IsTransparent;
                    var vList = isTransparent ? transparentVerts : opaqueVerts;
                    var iList = isTransparent ? transparentIndices : opaqueIndices;
                    ref var offset = ref isTransparent ? ref transparentIndexOffset : ref opaqueIndexOffset;

                    if (ShouldRenderFace(blocks, world, worldPosition, x, y, z - 1, isTransparent)) // North (-Z)
                    {
                        AddFace(vList, iList, ref offset, x, y, z, Direction.North, block.Id, uvResolver);
                    }

                    if (ShouldRenderFace(blocks, world, worldPosition, x, y, z + 1, isTransparent)) // South (+Z)
                    {
                        AddFace(vList, iList, ref offset, x, y, z, Direction.South, block.Id, uvResolver);
                    }

                    if (ShouldRenderFace(blocks, world, worldPosition, x + 1, y, z, isTransparent)) // East (+X)
                    {
                        AddFace(vList, iList, ref offset, x, y, z, Direction.East, block.Id, uvResolver);
                    }

                    if (ShouldRenderFace(blocks, world, worldPosition, x - 1, y, z, isTransparent)) // West (-X)
                    {
                        AddFace(vList, iList, ref offset, x, y, z, Direction.West, block.Id, uvResolver);
                    }

                    if (ShouldRenderFace(blocks, world, worldPosition, x, y + 1, z, isTransparent)) // Up (+Y)
                    {
                        AddFace(vList, iList, ref offset, x, y, z, Direction.Up, block.Id, uvResolver);
                    }

                    if (ShouldRenderFace(blocks, world, worldPosition, x, y - 1, z, isTransparent)) // Down (-Y)
                    {
                        AddFace(vList, iList, ref offset, x, y, z, Direction.Down, block.Id, uvResolver);
                    }
                }
            }
        }

        var opaque = new ChunkMesh { Vertices = opaqueVerts.ToArray(), Indices = opaqueIndices.ToArray() };
        var transparent = new ChunkMesh
        {
            Vertices = transparentVerts.ToArray(),
            Indices = transparentIndices.ToArray()
        };

        return (opaque, transparent);
    }

    private static bool ShouldRenderFace(Block[,,] blocks, IWorld world, Vector3 worldPosition,
        int x, int y, int z, bool currentIsTransparent)
    {
        // 1. Check within chunk bounds first
        if (x is >= 0 and < Chunk.Size && y is >= 0 and < Chunk.Height && z is >= 0 and < Chunk.Size)
        {
            var neighbor = blocks[x, y, z];
            if (neighbor.IsAir)
            {
                return true;
            }

            // Water only renders faces exposed to air (top + shoreline edges). It does NOT render an
            // underside against the solid bed — that dark downward face is what showed through clear
            // water. The bed renders its own top face instead (below), and since the water underside
            // is gone there's no coplanar pair to z-fight.
            if (currentIsTransparent)
            {
                return false;
            }

            // Solids render faces against air or ANY transparent neighbour (incl. water), so the
            // lake bed keeps a proper lit top face under clear water.
            return neighbor.IsTransparent;
        }

        // 2. Check neighboring chunk
        var worldX = (int)worldPosition.X + x;
        var worldZ = (int)worldPosition.Z + z;

        if (y is < 0 or >= Chunk.Height)
        {
            return true;
        }

        var neighborBlock = world.GetBlock(worldX, y, worldZ);
        if (neighborBlock.IsAir)
        {
            return true;
        }

        if (currentIsTransparent)
        {
            return false;
        }

        return neighborBlock.IsTransparent;
    }

    private static void AddFace(List<float> vertices, List<uint> indices, ref uint offset,
        int x, int y, int z, Direction dir, ushort blockId, Chunk.UvResolver uvResolver)
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
                faceVerts[0] = fx;
                faceVerts[1] = fy + 1;
                faceVerts[2] = fz;
                faceVerts[3] = fx;
                faceVerts[4] = fy + 1;
                faceVerts[5] = fz + 1;
                faceVerts[6] = fx + 1;
                faceVerts[7] = fy + 1;
                faceVerts[8] = fz + 1;
                faceVerts[9] = fx + 1;
                faceVerts[10] = fy + 1;
                faceVerts[11] = fz;
                break;
            case Direction.Down:
                faceVerts[0] = fx;
                faceVerts[1] = fy;
                faceVerts[2] = fz;
                faceVerts[3] = fx + 1;
                faceVerts[4] = fy;
                faceVerts[5] = fz;
                faceVerts[6] = fx + 1;
                faceVerts[7] = fy;
                faceVerts[8] = fz + 1;
                faceVerts[9] = fx;
                faceVerts[10] = fy;
                faceVerts[11] = fz + 1;
                break;
            case Direction.North:
                faceVerts[0] = fx + 1;
                faceVerts[1] = fy;
                faceVerts[2] = fz;
                faceVerts[3] = fx;
                faceVerts[4] = fy;
                faceVerts[5] = fz;
                faceVerts[6] = fx;
                faceVerts[7] = fy + 1;
                faceVerts[8] = fz;
                faceVerts[9] = fx + 1;
                faceVerts[10] = fy + 1;
                faceVerts[11] = fz;
                break;
            case Direction.South:
                faceVerts[0] = fx;
                faceVerts[1] = fy;
                faceVerts[2] = fz + 1;
                faceVerts[3] = fx + 1;
                faceVerts[4] = fy;
                faceVerts[5] = fz + 1;
                faceVerts[6] = fx + 1;
                faceVerts[7] = fy + 1;
                faceVerts[8] = fz + 1;
                faceVerts[9] = fx;
                faceVerts[10] = fy + 1;
                faceVerts[11] = fz + 1;
                break;
            case Direction.East:
                faceVerts[0] = fx + 1;
                faceVerts[1] = fy;
                faceVerts[2] = fz + 1;
                faceVerts[3] = fx + 1;
                faceVerts[4] = fy;
                faceVerts[5] = fz;
                faceVerts[6] = fx + 1;
                faceVerts[7] = fy + 1;
                faceVerts[8] = fz;
                faceVerts[9] = fx + 1;
                faceVerts[10] = fy + 1;
                faceVerts[11] = fz + 1;
                break;
            case Direction.West:
                faceVerts[0] = fx;
                faceVerts[1] = fy;
                faceVerts[2] = fz;
                faceVerts[3] = fx;
                faceVerts[4] = fy;
                faceVerts[5] = fz + 1;
                faceVerts[6] = fx;
                faceVerts[7] = fy + 1;
                faceVerts[8] = fz + 1;
                faceVerts[9] = fx;
                faceVerts[10] = fy + 1;
                faceVerts[11] = fz;
                break;
        }

        // Texture data
        Span<float> uvs = stackalloc float[8];
        uvResolver(blockId, dir, uvs);

        // Normals
        float nx = 0, ny = 0, nz = 0;
        switch (dir)
        {
            case Direction.Up:
                ny = 1f;
                break;
            case Direction.Down:
                ny = -1f;
                break;
            case Direction.North:
                nz = -1f;
                break;
            case Direction.South:
                nz = 1f;
                break;
            case Direction.East:
                nx = 1f;
                break;
            case Direction.West:
                nx = -1f;
                break;
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
