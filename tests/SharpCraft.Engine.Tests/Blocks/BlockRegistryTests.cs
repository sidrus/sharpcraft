using AwesomeAssertions;
using SharpCraft.Engine.Blocks;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Resources;

namespace SharpCraft.Engine.Tests.Blocks;

public class BlockRegistryTests
{
    private static BlockDefinition Def(string id)
    {
        return new BlockDefinition(new ResourceLocation("test", id), id);
    }

    [Fact]
    public void GetId_AssignsSequentialIdsFromOne()
    {
        var registry = new BlockRegistry();
        var grass = new ResourceLocation("test", "grass");
        var stone = new ResourceLocation("test", "stone");

        registry.Register(grass, Def("grass"));
        registry.Register(stone, Def("stone"));

        registry.GetId(grass).Should().Be(1);
        registry.GetId(stone).Should().Be(2);
    }

    [Fact]
    public void GetId_UnregisteredLocation_ShouldBeZero()
    {
        var registry = new BlockRegistry();

        registry.GetId(new ResourceLocation("test", "missing")).Should().Be(0);
    }

    [Fact]
    public void GetById_ReturnsDefinitionForAssignedId()
    {
        var registry = new BlockRegistry();
        var stone = new ResourceLocation("test", "stone");
        var definition = Def("stone");
        registry.Register(stone, definition);

        registry.GetById(registry.GetId(stone)).Should().Be(definition);
    }

    [Fact]
    public void GetById_ZeroOrUnknownId_ShouldBeNull()
    {
        var registry = new BlockRegistry();

        registry.GetById(0).Should().BeNull();
        registry.GetById(99).Should().BeNull();
    }
}