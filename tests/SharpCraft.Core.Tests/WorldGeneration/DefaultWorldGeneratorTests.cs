using SharpCraft.Core.Blocks;
using SharpCraft.Core.Numerics;
using SharpCraft.Core.WorldGeneration;
using AwesomeAssertions;

namespace SharpCraft.Core.Tests.WorldGeneration;

public class DefaultWorldGeneratorTests
{
    [Fact]
    public void GenerateChunk_ShouldPopulateChunkWithBlocks()
    {
        var generator = new DefaultWorldGenerator(12345);
        var chunk = new Chunk(new Vector2<int>(0, 0));

        generator.GenerateChunk(chunk);

        // Check a few points to ensure it's not all Air
        var hasNonAir = false;
        for (var x = 0; x < Chunk.Size; x++)
        {
            for (var z = 0; z < Chunk.Size; z++)
            {
                if (chunk.GetBlock(x, 0, z).Type != BlockType.Air)
                {
                    hasNonAir = true;
                    break;
                }
            }
            if (hasNonAir) break;
        }

        hasNonAir.Should().BeTrue();
    }

    [Fact]
    public void GenerateChunk_ShouldBeDeterministic_ForSameSeedAndCoord()
    {
        var generator1 = new DefaultWorldGenerator(12345);
        var chunk1 = new Chunk(new Vector2<int>(1, 1));
        generator1.GenerateChunk(chunk1);

        var generator2 = new DefaultWorldGenerator(12345);
        var chunk2 = new Chunk(new Vector2<int>(1, 1));
        generator2.GenerateChunk(chunk2);

        for (var x = 0; x < Chunk.Size; x++)
        {
            for (var y = 0; y < Chunk.Height; y++)
            {
                for (var z = 0; z < Chunk.Size; z++)
                {
                    chunk1.GetBlock(x, y, z).Type.Should().Be(chunk2.GetBlock(x, y, z).Type);
                }
            }
        }
    }

    [Fact]
    public void GenerateChunk_ShouldRespectWorldPosition()
    {
        var generator = new DefaultWorldGenerator(12345);
        
        // Chunk at (0,0)
        var chunk1 = new Chunk(new Vector2<int>(0, 0));
        generator.GenerateChunk(chunk1);

        // Chunk at (1,0) but we check blocks that should be identical if they were at same world pos
        // Actually, we can check that a block at (15, y, 0) in chunk (0,0) 
        // is different from block at (0, y, 0) in chunk (1,0) if the noise is changing.
        // Or more simply, just check they are different.
        var chunk2 = new Chunk(new Vector2<int>(10, 10));
        generator.GenerateChunk(chunk2);

        var identical = true;
        for (var x = 0; x < Chunk.Size; x++)
        {
            for (var y = 0; y < Chunk.Height; y++)
            {
                for (var z = 0; z < Chunk.Size; z++)
                {
                    if (chunk1.GetBlock(x, y, z).Type != chunk2.GetBlock(x, y, z).Type)
                    {
                        identical = false;
                        break;
                    }
                }
                if (!identical) break;
            }
            if (!identical) break;
        }

        identical.Should().BeFalse();
    }
}
