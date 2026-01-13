using FluentAssertions;
using SharpCraft.Core.Blocks;
using SharpCraft.Core.WorldGeneration;
using SharpCraft.Sdk.Runtime.World;
using SharpCraft.Sdk.World;
using Xunit;
using SharpCraft.Core.Numerics;

namespace SharpCraft.Sdk.Tests;

public class WorldGenerationTests
{
    private class FlatWorldGenerator : SharpCraft.Sdk.World.IWorldGenerator
    {
        public void GenerateChunk(IChunkData chunk, long seed)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    chunk.SetBlock(x, 0, z, "sharpcraft:bedrock");
                    for (int y = 1; y < 4; y++)
                    {
                        chunk.SetBlock(x, y, z, "sharpcraft:stone");
                    }
                    chunk.SetBlock(x, 4, z, "sharpcraft:grass");
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
        chunk.GetBlock(0, 0, 0).Type.Should().Be(BlockType.Bedrock);
        chunk.GetBlock(0, 1, 0).Type.Should().Be(BlockType.Stone);
        chunk.GetBlock(0, 4, 0).Type.Should().Be(BlockType.Grass);
        chunk.GetBlock(0, 5, 0).Type.Should().Be(BlockType.Air);
    }

    [Fact]
    public void WorldGenerationRegistry_ShouldRegisterGenerator()
    {
        var registry = new WorldGenerationRegistry();
        var generator = new FlatWorldGenerator();

        registry.Register("test:flat", generator);

        registry.Get("test:flat").Should().Be(generator);
    }
}
