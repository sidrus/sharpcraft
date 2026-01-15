using System.Numerics;
using SharpCraft.Sdk.Numerics;
using AwesomeAssertions;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Engine.Universe;
using Moq;
using WorldClass = SharpCraft.Engine.Universe.World;

namespace SharpCraft.Engine.Tests;

public class WorldTests
{
    private static WorldClass CreateWorld() => new(Mock.Of<SharpCraft.Sdk.Universe.IWorldGenerator>(), 0, Mock.Of<IBlockRegistry>());

    [Fact]
    public void GetOrCreateChunk_ShouldCreateNewChunk()
    {
        var world = CreateWorld();
        var coord = new Vector2<int>(1, 2);

        var chunk = world.GetOrCreateChunk(coord);

        chunk.Should().NotBeNull();
        world.GetLoadedChunks().Should().Contain(chunk);
    }

    [Fact]
    public void GetOrCreateChunk_ShouldReturnExistingChunk()
    {
        var world = CreateWorld();
        var coord = new Vector2<int>(1, 2);

        var chunk1 = world.GetOrCreateChunk(coord);
        var chunk2 = world.GetOrCreateChunk(coord);

        chunk1.Should().BeSameAs(chunk2);
    }

    [Fact]
    public void GetBlock_ShouldReturnBlockFromCorrectChunk()
    {
        var world = CreateWorld();
        // Set a block at a specific world coordinate
        // (16, 64, 16) is in chunk (1, 1) at local (0, 64, 0)
        world.SetBlock(16, 64, 16, BlockType.Stone);

        var block = world.GetBlock(16, 64, 16);

        block.Type.Should().Be(BlockType.Stone);
    }

    [Fact]
    public void GetBlock_ShouldReturnAir_ForUnloadedChunk()
    {
        var world = CreateWorld();
        
        var block = world.GetBlock(1000, 64, 1000);

        block.Type.Should().Be(BlockType.Air);
    }

    [Fact]
    public void Generate_ShouldCreateChunksInBounds()
    {
        var world = CreateWorld();
        var bounds = 1; // -1 to 1 inclusive -> 3x3 = 9 chunks

        world.Generate(bounds);

        world.GetLoadedChunks().Should().HaveCount(9);
    }

    [Fact]
    public async Task GenerateAsync_ShouldCreateChunksProgressively()
    {
        var world = CreateWorld();
        var bounds = 1;

        await world.GenerateAsync(bounds);

        world.GetLoadedChunks().Should().HaveCount(9);
    }

    [Fact]
    public void UnloadChunks_ShouldRemoveChunksOutsideRange()
    {
        var world = CreateWorld();
        world.Generate(2); // 5x5 = 25 chunks
        world.GetLoadedChunks().Should().HaveCount(25);

        // Unload everything further than 1 chunk from (0,0,0)
        world.UnloadChunks(Vector3.Zero, 1);

        // Should keep chunks from -1 to 1 (3x3 = 9 chunks)
        world.GetLoadedChunks().Should().HaveCount(9);
    }

    [Fact]
    public void SetBlock_ShouldCreateChunkIfMissing()
    {
        var world = CreateWorld();
        var wx = 32;
        var wy = 64;
        var wz = 32;

        world.GetLoadedChunks().Should().BeEmpty();

        world.SetBlock(wx, wy, wz, BlockType.Dirt);

        world.GetLoadedChunks().Should().HaveCount(1);
        world.GetBlock(wx, wy, wz).Type.Should().Be(BlockType.Dirt);
    }
}
