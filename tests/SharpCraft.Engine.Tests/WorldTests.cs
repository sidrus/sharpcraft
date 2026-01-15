using System.Numerics;
using SharpCraft.Sdk.Numerics;
using AwesomeAssertions;
using SharpCraft.Engine.Blocks;
using SharpCraft.Sdk.Blocks;
using WorldClass = SharpCraft.Engine.Universe.World;

namespace SharpCraft.Engine.Tests;

public class WorldTests
{
    private static IBlockRegistry CreateRegistry()
    {
        var registry = new BlockRegistry();
        registry.Register(BlockIds.Air, new BlockDefinition(BlockIds.Air, "Air", IsSolid: false, IsTransparent: true));
        registry.Register(BlockIds.Stone, new BlockDefinition(BlockIds.Stone, "Stone"));
        registry.Register(BlockIds.Dirt, new BlockDefinition(BlockIds.Dirt, "Dirt"));
        return registry;
    }

    [Fact]
    public void GetOrCreateChunk_ShouldCreateNewChunk()
    {
        var world = new WorldClass(CreateRegistry());
        var coord = new Vector2<int>(1, 2);

        var chunk = world.GetOrCreateChunk(coord);

        chunk.Should().NotBeNull();
        world.GetLoadedChunks().Should().Contain(chunk);
    }

    [Fact]
    public void GetOrCreateChunk_ShouldReturnExistingChunk()
    {
        var world = new WorldClass(CreateRegistry());
        var coord = new Vector2<int>(1, 2);

        var chunk1 = world.GetOrCreateChunk(coord);
        var chunk2 = world.GetOrCreateChunk(coord);

        chunk1.Should().BeSameAs(chunk2);
    }

    [Fact]
    public void GetBlock_ShouldReturnBlockFromCorrectChunk()
    {
        var world = new WorldClass(CreateRegistry());
        // Set a block at a specific world coordinate
        // (16, 64, 16) is in chunk (1, 1) at local (0, 64, 0)
        world.SetBlock(16, 64, 16, BlockIds.Stone);

        var block = world.GetBlock(16, 64, 16);

        block.Id.Should().Be(BlockIds.Stone);
    }

    [Fact]
    public void GetBlock_ShouldReturnAir_ForUnloadedChunk()
    {
        var world = new WorldClass(CreateRegistry());
        
        var block = world.GetBlock(1000, 64, 1000);

        block.Id.Should().Be(BlockIds.Air);
    }

    [Fact]
    public void Generate_ShouldCreateChunksInBounds()
    {
        var world = new WorldClass(CreateRegistry());
        var bounds = 1; // -1 to 1 inclusive -> 3x3 = 9 chunks

        world.Generate(bounds);

        world.GetLoadedChunks().Should().HaveCount(9);
    }

    [Fact]
    public async Task GenerateAsync_ShouldCreateChunksProgressively()
    {
        var world = new WorldClass(CreateRegistry());
        var bounds = 1;

        await world.GenerateAsync(bounds);

        world.GetLoadedChunks().Should().HaveCount(9);
    }

    [Fact]
    public void UnloadChunks_ShouldRemoveChunksOutsideRange()
    {
        var world = new WorldClass(CreateRegistry());
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
        var world = new WorldClass(CreateRegistry());
        var wx = 32;
        var wy = 64;
        var wz = 32;

        world.GetLoadedChunks().Should().BeEmpty();

        world.SetBlock(wx, wy, wz, BlockIds.Dirt);

        world.GetLoadedChunks().Should().HaveCount(1);
        world.GetBlock(wx, wy, wz).Id.Should().Be(BlockIds.Dirt);
    }
}
