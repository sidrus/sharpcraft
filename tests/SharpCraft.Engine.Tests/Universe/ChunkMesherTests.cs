using AwesomeAssertions;
using NSubstitute;
using SharpCraft.Engine.Universe;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Universe;
using System.Numerics;

namespace SharpCraft.Engine.Tests.Universe;

public class ChunkMesherTests
{
    private static readonly Block Solid = new(1, BlockFlags.Solid);
    private static readonly Block Water = new(2, BlockFlags.Transparent | BlockFlags.Fluid);

    private static Block[,,] EmptyChunk() => new Block[Chunk.Size, Chunk.Height, Chunk.Size];

    private static void NoUvs(ushort blockId, Direction dir, Span<float> uvs)
    {
    }

    private static int FaceCount(ChunkMesh mesh) => mesh.Indices.Length / 6;

    [Fact]
    public void Generate_IsolatedSolidBlock_ShouldEmitSixOpaqueFaces()
    {
        var blocks = EmptyChunk();
        blocks[1, 1, 1] = Solid;
        var world = Substitute.For<IWorld>();

        var result = ChunkMesher.Generate(blocks, Vector3.Zero, world, NoUvs);

        FaceCount(result.Opaque).Should().Be(6);
        FaceCount(result.Transparent).Should().Be(0);
    }

    [Fact]
    public void Generate_SixFloatsPerVertexEightStride_ShouldMatchLayout()
    {
        var blocks = EmptyChunk();
        blocks[1, 1, 1] = Solid;
        var world = Substitute.For<IWorld>();

        var result = ChunkMesher.Generate(blocks, Vector3.Zero, world, NoUvs);

        result.Opaque.Vertices.Length.Should().Be(6 * 4 * 8);
        result.Opaque.Indices.Length.Should().Be(6 * 6);
    }

    [Fact]
    public void Generate_AdjacentSolidBlocks_ShouldCullSharedFace()
    {
        var blocks = EmptyChunk();
        blocks[1, 1, 1] = Solid;
        blocks[2, 1, 1] = Solid;
        var world = Substitute.For<IWorld>();

        var result = ChunkMesher.Generate(blocks, Vector3.Zero, world, NoUvs);

        FaceCount(result.Opaque).Should().Be(10);
    }

    [Fact]
    public void Generate_EmptyChunk_ShouldEmitNothing()
    {
        var world = Substitute.For<IWorld>();

        var result = ChunkMesher.Generate(EmptyChunk(), Vector3.Zero, world, NoUvs);

        FaceCount(result.Opaque).Should().Be(0);
        FaceCount(result.Transparent).Should().Be(0);
    }

    [Fact]
    public void Generate_TransparentBlock_ShouldRouteToTransparentMesh()
    {
        var blocks = EmptyChunk();
        blocks[1, 1, 1] = Water;
        var world = Substitute.For<IWorld>();

        var result = ChunkMesher.Generate(blocks, Vector3.Zero, world, NoUvs);

        FaceCount(result.Transparent).Should().Be(6);
        FaceCount(result.Opaque).Should().Be(0);
    }

    [Fact]
    public void Generate_SolidBlockAgainstSolidNeighborChunk_ShouldCullBoundaryFace()
    {
        var blocks = EmptyChunk();
        blocks[0, 1, 1] = Solid;
        var world = Substitute.For<IWorld>();
        world.GetBlock(-1, 1, 1).Returns(Solid);

        var result = ChunkMesher.Generate(blocks, Vector3.Zero, world, NoUvs);

        FaceCount(result.Opaque).Should().Be(5);
    }

    [Fact]
    public void Generate_SolidBlockAgainstAirNeighborChunk_ShouldRenderBoundaryFace()
    {
        var blocks = EmptyChunk();
        blocks[0, 1, 1] = Solid;
        var world = Substitute.For<IWorld>();

        var result = ChunkMesher.Generate(blocks, Vector3.Zero, world, NoUvs);

        FaceCount(result.Opaque).Should().Be(6);
    }
}