using AwesomeAssertions;
using SharpCraft.Engine;
using SharpCraft.Engine.Blocks;
using SharpCraft.Engine.Universe;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Numerics;
using SharpCraft.Sdk.Resources;
using SharpCraft.Sdk.Universe;
using NSubstitute;
using IWorldGenerator = SharpCraft.Sdk.Universe.IWorldGenerator;

namespace SharpCraft.Sdk.Tests;

public class WorldGenerationTests
{
    private class FlatWorldGenerator : IWorldGenerator
    {
        public void GenerateChunk(IChunkData chunk, long seed)
        {
            for (var x = 0; x < 16; x++)
            {
                for (var z = 0; z < 16; z++)
                {
                    chunk.SetBlock(x, 0, z, "sharpcraft:bedrock");
                    for (var y = 1; y < 4; y++)
                    {
                        chunk.SetBlock(x, y, z, "sharpcraft:stone");
                    }
                    chunk.SetBlock(x, 4, z, "sharpcraft:grass");
                }
            }
        }
    }

    [Fact]
    public void Chunk_ShouldPopulateDirectly()
    {
        // Arrange
        var sdkGenerator = new FlatWorldGenerator();
        var bedrock = new ResourceLocation("sharpcraft", "bedrock");
        var stone = new ResourceLocation("sharpcraft", "stone");
        var grass = new ResourceLocation("sharpcraft", "grass");
        var blockRegistry = new BlockRegistry();
        blockRegistry.Register(bedrock, new BlockDefinition(bedrock, "Bedrock"));
        blockRegistry.Register(stone, new BlockDefinition(stone, "Stone"));
        blockRegistry.Register(grass, new BlockDefinition(grass, "Grass"));
        var chunk = new Chunk(new Vector2<int>(0, 0), blockRegistry);

        // Act
        sdkGenerator.GenerateChunk(chunk, 12345);

        // Assert
        chunk.GetBlock(0, 0, 0).Id.Should().Be(blockRegistry.GetId(bedrock));
        chunk.GetBlock(0, 1, 0).Id.Should().Be(blockRegistry.GetId(stone));
        chunk.GetBlock(0, 4, 0).Id.Should().Be(blockRegistry.GetId(grass));
        chunk.GetBlock(0, 5, 0).IsAir.Should().BeTrue();
    }

    [Fact]
    public void WorldGenerationRegistry_ShouldRegisterGenerator()
    {
        var registry = new Registry<IWorldGenerator>();
        var generator = new FlatWorldGenerator();

        registry.Register("test:flat", generator);

        registry.Get("test:flat").Should().Be(generator);
    }

    [Fact]
    public void WorldGenerationRegistry_TryGet_ShouldReturnTrueIfFound()
    {
        var registry = new Registry<IWorldGenerator>();
        var generator = new FlatWorldGenerator();
        registry.Register("test:flat", generator);

        var result = registry.TryGet("test:flat", out var found);

        result.Should().BeTrue();
        found.Should().Be(generator);
    }

    [Fact]
    public void WorldGenerationRegistry_TryGet_ShouldReturnFalseIfNotFound()
    {
        var registry = new Registry<IWorldGenerator>();

        var result = registry.TryGet("test:nonexistent", out var found);

        result.Should().BeFalse();
        found.Should().BeNull();
    }

    [Fact]
    public void WorldGenerationRegistry_All_ShouldReturnAllRegisteredGenerators()
    {
        var registry = new Registry<IWorldGenerator>();
        var generator1 = new FlatWorldGenerator();
        var generator2 = new FlatWorldGenerator();
        registry.Register("test:1", generator1);
        registry.Register("test:2", generator2);

        registry.All.Should().HaveCount(2);
        registry.All.Should().Contain(new KeyValuePair<ResourceLocation, IWorldGenerator>("test:1", generator1));
        registry.All.Should().Contain(new KeyValuePair<ResourceLocation, IWorldGenerator>("test:2", generator2));
    }

    [Fact]
    public void WorldGenerationRegistry_RegisterDuplicate_ShouldThrow()
    {
        var registry = new Registry<IWorldGenerator>();
        var generator = new FlatWorldGenerator();
        registry.Register("test:flat", generator);

        var act = () => registry.Register("test:flat", generator);

        act.Should().Throw<ArgumentException>().WithMessage("*already registered*");
    }
}
