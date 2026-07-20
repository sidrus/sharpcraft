using AwesomeAssertions;
using SharpCraft.Sdk.Blocks;

namespace SharpCraft.Sdk.Tests.Blocks;

public class BlockDefinitionTests
{
    [Fact]
    public void Flags_ShouldExposeProvidedFlags()
    {
        var def = new BlockDefinition("test:x", "X", Flags: BlockFlags.Transparent | BlockFlags.Fluid);

        def.Flags.Should().Be(BlockFlags.Transparent | BlockFlags.Fluid);
    }

    [Fact]
    public void Flags_WhenUnspecified_ShouldDefaultToSolid()
    {
        new BlockDefinition("test:x", "X").Flags.Should().Be(BlockFlags.Solid);
    }

    [Fact]
    public void Fluid_ShouldBeStoredAlongsideFlags()
    {
        var water = new FluidProperties(1000f, 0.15f, -2f, 2f, 4f, 0.8f, -2f, 0.5f);

        var def = new BlockDefinition("test:water", "Water", Flags: BlockFlags.Fluid, Fluid: water);

        def.Fluid.Should().Be(water);
    }
}
