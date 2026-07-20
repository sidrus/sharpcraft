using AwesomeAssertions;
using SharpCraft.Engine.Blocks;
using SharpCraft.Engine.Rendering.Textures;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Resources;

namespace SharpCraft.Client.Tests;

public class BlockAtlasTests
{
    private sealed class FakeAtlas(ResourceLocation known, (float, float, float, float) rect) : IAtlasUvs
    {
        public bool TryGetUvs(ResourceLocation location, out (float U, float V, float Width, float Height) uv)
        {
            if (location == known)
            {
                uv = rect;
                return true;
            }

            uv = default;
            return false;
        }
    }

    [Fact]
    public void ResolveUvs_KnownTopFace_ShouldWriteAtlasCorners()
    {
        var texture = new ResourceLocation("test", "grass_top");
        var grass = new ResourceLocation("test", "grass");
        var blocks = new BlockRegistry();
        blocks.Register(grass, new BlockDefinition(grass, "Grass", TextureTop: texture));
        var atlas = new BlockAtlas(new FakeAtlas(texture, (0.1f, 0.2f, 0.5f, 0.5f)), blocks);

        Span<float> uvs = stackalloc float[8];
        atlas.ResolveUvs(blocks.GetId(grass), Direction.Up, uvs);

        uvs.ToArray().Should().Equal(0.1f, 0.7f, 0.6f, 0.7f, 0.6f, 0.2f, 0.1f, 0.2f);
    }

    [Fact]
    public void ResolveUvs_UnknownBlock_ShouldWriteZeros()
    {
        var blocks = new BlockRegistry();
        var atlas = new BlockAtlas(new FakeAtlas(default, default), blocks);

        Span<float> uvs = stackalloc float[8];
        uvs.Fill(9f);
        atlas.ResolveUvs(999, Direction.North, uvs);

        uvs.ToArray().Should().AllBeEquivalentTo(0f);
    }
}