namespace SharpCraft.Engine.Rendering.Textures;

/// <summary>
/// Resolves per-face atlas UVs for a block id: looks up its <see cref="BlockDefinition"/>,
/// picks the face texture, and finds its atlas rectangle.
/// </summary>
public sealed class BlockAtlas(IAtlasUvs atlas, IBlockRegistry blocks)
{
    /// <summary>
    /// Writes the 4 UV corners (8 floats) for the given block face into <paramref name="uvs"/>,
    /// or zeros when the block or texture is unknown.
    /// </summary>
    public void ResolveUvs(ushort blockId, Direction dir, Span<float> uvs)
    {
        var def = blocks.GetById(blockId);
        if (def == null)
        {
            uvs.Clear();
            return;
        }

        var loc = dir switch
        {
            Direction.Up => def.TextureTop ?? def.TextureSides,
            Direction.Down => def.TextureBottom ?? def.TextureSides,
            _ => def.TextureSides
        };

        if (loc is { } location && atlas.TryGetUvs(location, out var uvRect))
        {
            uvs[0] = uvRect.U;
            uvs[1] = uvRect.V + uvRect.Height;
            uvs[2] = uvRect.U + uvRect.Width;
            uvs[3] = uvRect.V + uvRect.Height;
            uvs[4] = uvRect.U + uvRect.Width;
            uvs[5] = uvRect.V;
            uvs[6] = uvRect.U;
            uvs[7] = uvRect.V;
        }
        else
        {
            uvs.Clear();
        }
    }
}