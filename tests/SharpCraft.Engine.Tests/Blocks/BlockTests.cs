using AwesomeAssertions;
using SharpCraft.Sdk.Blocks;

namespace SharpCraft.Engine.Tests.Blocks;

public class BlockTests
{
    [Fact]
    public void IsAir_ShouldReturnTrue_ForAirBlock()
    {
        var block = new Block { Id = BlockIds.Air };
        block.IsAir.Should().BeTrue();
    }

    [Fact]
    public void IsAir_ShouldReturnTrue_ForNullId()
    {
        var block = new Block { Id = null! };
        block.IsAir.Should().BeTrue();
    }

    [Fact]
    public void IsAir_ShouldReturnFalse_ForNonAirBlock()
    {
        var block = new Block { Id = BlockIds.Stone };
        block.IsAir.Should().BeFalse();
    }
}
