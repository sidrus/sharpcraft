using AwesomeAssertions;
using SharpCraft.Sdk.Blocks;

namespace SharpCraft.Engine.Tests.Blocks;

public class BlockTests
{
    [Fact]
    public void IsAir_Id0_ShouldBeTrue()
    {
        new Block(0, BlockFlags.None).IsAir.Should().BeTrue();
    }

    [Fact]
    public void IsAir_NonZeroId_ShouldBeFalse()
    {
        new Block(1, BlockFlags.Solid).IsAir.Should().BeFalse();
    }

    [Theory]
    [InlineData(BlockFlags.Solid, true)]
    [InlineData(BlockFlags.Transparent, false)]
    [InlineData(BlockFlags.None, false)]
    public void IsSolid_ShouldReflectFlag(BlockFlags flags, bool expected)
    {
        new Block(1, flags).IsSolid.Should().Be(expected);
    }

    [Theory]
    [InlineData(BlockFlags.Transparent, true)]
    [InlineData(BlockFlags.Solid, false)]
    public void IsTransparent_ShouldReflectFlag(BlockFlags flags, bool expected)
    {
        new Block(1, flags).IsTransparent.Should().Be(expected);
    }

    [Theory]
    [InlineData(BlockFlags.Fluid, true)]
    [InlineData(BlockFlags.Solid, false)]
    public void IsFluid_ShouldReflectFlag(BlockFlags flags, bool expected)
    {
        new Block(1, flags).IsFluid.Should().Be(expected);
    }
}
