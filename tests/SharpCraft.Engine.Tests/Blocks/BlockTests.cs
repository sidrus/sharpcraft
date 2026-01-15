using AwesomeAssertions;
using SharpCraft.Sdk.Blocks;

namespace SharpCraft.Engine.Tests.Blocks;

public class BlockTests
{
    [Theory]
    [InlineData(BlockType.Air, false)]
    [InlineData(BlockType.Water, false)]
    [InlineData(BlockType.Lava, false)]
    [InlineData(BlockType.Stone, true)]
    [InlineData(BlockType.Grass, true)]
    [InlineData(BlockType.Dirt, true)]
    [InlineData(BlockType.Sand, true)]
    [InlineData(BlockType.Bedrock, true)]
    public void IsSolid_ShouldReturnExpectedValue(BlockType type, bool expected)
    {
        var block = new Block { Type = type };

        block.IsSolid.Should().Be(expected);
    }

    [Theory]
    [InlineData(BlockType.Air, true)]
    [InlineData(BlockType.Water, true)]
    [InlineData(BlockType.Stone, false)]
    [InlineData(BlockType.Grass, false)]
    public void IsTransparent_ShouldReturnExpectedValue(BlockType type, bool expected)
    {
        var block = new Block { Type = type };

        block.IsTransparent.Should().Be(expected);
    }

    [Theory]
    [InlineData(BlockType.Grass, 0.5f)]
    [InlineData(BlockType.Dirt, 0.5f)]
    [InlineData(BlockType.Stone, 0.5f)]
    [InlineData(BlockType.Air, 0.01f)]
    [InlineData(BlockType.Water, 1f)]
    [InlineData(BlockType.Bedrock, 0.8f)]
    public void Friction_ShouldReturnExpectedValue(BlockType type, float expected)
    {
        var block = new Block { Type = type };

        block.Friction.Should().Be(expected);
    }
}
