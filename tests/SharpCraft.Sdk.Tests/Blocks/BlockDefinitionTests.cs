using AwesomeAssertions;
using SharpCraft.Sdk.Blocks;

namespace SharpCraft.Sdk.Tests.Blocks;

public class BlockDefinitionTests
{
    private static BlockDefinition Def(bool solid, bool transparent, FluidProperties? fluid = null)
    {
        return new BlockDefinition("test:x", "X", IsSolid: solid, IsTransparent: transparent, Fluid: fluid);
    }

    [Fact]
    public void Flags_SolidOpaque_ShouldBeSolidOnly()
    {
        Def(solid: true, transparent: false).Flags.Should().Be(BlockFlags.Solid);
    }

    [Fact]
    public void Flags_TransparentFluid_ShouldBeTransparentAndFluid()
    {
        var water = new FluidProperties(1000f, 0.15f, -2f, 2f, 4f, 0.8f, -2f, 0.5f);

        Def(solid: false, transparent: true, fluid: water).Flags
            .Should().Be(BlockFlags.Transparent | BlockFlags.Fluid);
    }

    [Fact]
    public void Flags_NonSolidNonTransparent_ShouldBeNone()
    {
        Def(solid: false, transparent: false).Flags.Should().Be(BlockFlags.None);
    }
}