namespace SharpCraft.Engine.Rendering.Textures;

/// <summary>
/// Resolves the atlas sub-rectangle for a texture by its resource location.
/// </summary>
public interface IAtlasUvs
{
    /// <summary>
    /// Attempts to get the atlas UV rectangle for the given texture location.
    /// </summary>
    /// <param name="location">The texture resource location.</param>
    /// <param name="uv">The atlas rectangle (origin U/V, Width/Height) when found.</param>
    /// <returns>True if the texture is present in the atlas; otherwise false.</returns>
    bool TryGetUvs(ResourceLocation location, out (float U, float V, float Width, float Height) uv);
}