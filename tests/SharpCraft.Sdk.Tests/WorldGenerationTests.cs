using AwesomeAssertions;
using SharpCraft.Engine.Universe;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Numerics;
using SharpCraft.Sdk.Resources;
using SharpCraft.Sdk.Universe;
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
                    chunk.SetBlock(x, 0, z, BlockIds.Bedrock);
                    for (var y = 1; y < 4; y++)
                    {
                        chunk.SetBlock(x, y, z, BlockIds.Stone);
                    }
                    chunk.SetBlock(x, 4, z, BlockIds.Grass);
                }
            }
        }
    }

    [Fact]
    public void SdkWorldGeneratorBridge_ShouldPopulateChunk()
    {
        // Arrange
        var sdkGenerator = new FlatWorldGenerator();
        var bridge = new SdkWorldGeneratorBridge(sdkGenerator, 12345);
        var chunk = new Chunk(new Vector2<int>(0, 0));

        // Act
        bridge.GenerateChunk(chunk);

        // Assert
        chunk.GetBlock(0, 0, 0).Id.Should().Be(BlockIds.Bedrock);
        chunk.GetBlock(0, 1, 0).Id.Should().Be(BlockIds.Stone);
        chunk.GetBlock(0, 4, 0).Id.Should().Be(BlockIds.Grass);
        chunk.GetBlock(0, 5, 0).Id.Should().Be(BlockIds.Air);
    }

    [Fact]
    public void WorldGenerationRegistry_ShouldRegisterGenerator()
    {
        var registry = new WorldGenerationRegistry();
        var generator = new FlatWorldGenerator();

        registry.Register("test:flat", generator);

        registry.Get("test:flat").Should().Be(generator);
    }

    [Fact]
    public void WorldGenerationRegistry_TryGet_ShouldReturnTrueIfFound()
    {
        var registry = new WorldGenerationRegistry();
        var generator = new FlatWorldGenerator();
        registry.Register("test:flat", generator);

        var result = registry.TryGet("test:flat", out var found);

        result.Should().BeTrue();
        found.Should().Be(generator);
    }

    [Fact]
    public void WorldGenerationRegistry_TryGet_ShouldReturnFalseIfNotFound()
    {
        var registry = new WorldGenerationRegistry();

        var result = registry.TryGet("test:nonexistent", out var found);

        result.Should().BeFalse();
        found.Should().BeNull();
    }

    [Fact]
    public void WorldGenerationRegistry_All_ShouldReturnAllRegisteredGenerators()
    {
        var registry = new WorldGenerationRegistry();
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
        var registry = new WorldGenerationRegistry();
        var generator = new FlatWorldGenerator();
        registry.Register("test:flat", generator);

        var act = () => registry.Register("test:flat", generator);

        act.Should().Throw<ArgumentException>().WithMessage("*already registered*");
    }
}
