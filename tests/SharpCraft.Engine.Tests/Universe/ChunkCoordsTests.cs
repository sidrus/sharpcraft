using AwesomeAssertions;
using SharpCraft.Engine.Universe;

namespace SharpCraft.Engine.Tests.Universe;

public class ChunkCoordsTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(15, 0)]
    [InlineData(16, 1)]
    [InlineData(31, 1)]
    [InlineData(-1, -1)]
    [InlineData(-16, -1)]
    [InlineData(-17, -2)]
    public void ToChunk_ShouldFloorDivideBySize(int world, int expectedChunk)
    {
        ChunkCoords.ToChunk(world).Should().Be(expectedChunk);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(15, 15)]
    [InlineData(16, 0)]
    [InlineData(17, 1)]
    [InlineData(-1, 15)]
    [InlineData(-16, 0)]
    public void ToLocal_ShouldWrapWithinChunk(int world, int expectedLocal)
    {
        ChunkCoords.ToLocal(world).Should().Be(expectedLocal);
    }

    [Theory]
    [InlineData(0.0f, 0)]
    [InlineData(15.9f, 0)]
    [InlineData(16.1f, 1)]
    [InlineData(-0.1f, -1)]
    [InlineData(-16.0f, -1)]
    public void ToChunk_FromWorldPosition_ShouldMatchFloorDivide(float world, int expectedChunk)
    {
        ChunkCoords.ToChunk(world).Should().Be(expectedChunk);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(255, true)]
    [InlineData(-1, false)]
    [InlineData(256, false)]
    public void IsWithinHeight_ShouldBoundToChunkHeight(int worldY, bool expected)
    {
        ChunkCoords.IsWithinHeight(worldY).Should().Be(expected);
    }

    [Fact]
    public void SizeLog2_ShouldBeConsistentWithSize()
    {
        (1 << Chunk.SizeLog2).Should().Be(Chunk.Size);
    }
}
