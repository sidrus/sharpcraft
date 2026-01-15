using SharpCraft.Sdk.Assets;
using SharpCraft.Sdk.Resources;
using Silk.NET.OpenGL;

namespace SharpCraft.Client.Rendering.Textures;

/// <summary>
/// A dynamic texture atlas that aggregates textures from the asset registry.
/// </summary>
public class TextureAtlas(GL gl, IAssetRegistry assets) : IDisposable
{
    private readonly Dictionary<ResourceLocation, (float U, float V, float Width, float Height)> _uvs = new();
    private Texture2d? _diffuseAtlas;
    private Texture2d? _normalAtlas;
    private Texture2d? _aoAtlas;
    private Texture2d? _specularAtlas;
    private Texture2d? _metallicAtlas;
    private Texture2d? _roughnessAtlas;

    public void Build()
    {
        var textures = assets.All.ToList();
        if (textures.Count == 0) return;

        var maxTileW = textures.Max(t => t.Value.Width);
        var maxTileH = textures.Max(t => t.Value.Height);

        // Simple square atlas layout
        var tilesPerRow = (int)Math.Ceiling(Math.Sqrt(textures.Count));
        var atlasWidth = tilesPerRow * maxTileW;
        var atlasHeight = (int)Math.Ceiling((float)textures.Count / tilesPerRow) * maxTileH;

        var diffuseData = new byte[atlasWidth * atlasHeight * 4];
        var normalData = new byte[atlasWidth * atlasHeight * 4];
        var aoData = new byte[atlasWidth * atlasHeight * 4];
        var specularData = new byte[atlasWidth * atlasHeight * 4];
        var metallicData = new byte[atlasWidth * atlasHeight * 4];
        var roughnessData = new byte[atlasWidth * atlasHeight * 4];

        for (var i = 0; i < textures.Count; i++)
        {
            var (location, data) = textures[i];
            var row = i / tilesPerRow;
            var col = i % tilesPerRow;

            var xOffset = col * maxTileW;
            var yOffset = row * maxTileH;

            CopyLayer(data.Data, diffuseData, xOffset, yOffset, atlasWidth, data.Width, data.Height);
            
            if (data.NormalData != null)
                CopyLayer(data.NormalData, normalData, xOffset, yOffset, atlasWidth, data.Width, data.Height);
            else
                FillLayer(normalData, xOffset, yOffset, atlasWidth, data.Width, data.Height, 128, 128, 255, 255); // Default flat normal

            if (data.AoData != null)
                CopyLayer(data.AoData, aoData, xOffset, yOffset, atlasWidth, data.Width, data.Height);
            else
                FillLayer(aoData, xOffset, yOffset, atlasWidth, data.Width, data.Height, 255, 255, 255, 255); // Default white AO

            if (data.SpecularData != null)
                CopyLayer(data.SpecularData, specularData, xOffset, yOffset, atlasWidth, data.Width, data.Height);
            else
                FillLayer(specularData, xOffset, yOffset, atlasWidth, data.Width, data.Height, 0, 0, 0, 255); // Default no specular

            if (data.MetallicData != null)
                CopyLayer(data.MetallicData, metallicData, xOffset, yOffset, atlasWidth, data.Width, data.Height);
            else
                FillLayer(metallicData, xOffset, yOffset, atlasWidth, data.Width, data.Height, 0, 0, 0, 255); // Default to black (non-metallic)

            if (data.RoughnessData != null)
                CopyLayer(data.RoughnessData, roughnessData, xOffset, yOffset, atlasWidth, data.Width, data.Height);
            else
                FillLayer(roughnessData, xOffset, yOffset, atlasWidth, data.Width, data.Height, 255, 255, 255, 255); // Default full roughness

            _uvs[location] = (
                (float)xOffset / atlasWidth,
                (float)yOffset / atlasHeight,
                (float)data.Width / atlasWidth,
                (float)data.Height / atlasHeight
            );
        }

        _diffuseAtlas?.Dispose();
        _normalAtlas?.Dispose();
        _aoAtlas?.Dispose();
        _specularAtlas?.Dispose();
        _metallicAtlas?.Dispose();
        _roughnessAtlas?.Dispose();

        _diffuseAtlas = new Texture2d(gl, atlasWidth, atlasHeight, diffuseData, InternalFormat.SrgbAlpha);
        _normalAtlas = new Texture2d(gl, atlasWidth, atlasHeight, normalData, InternalFormat.Rgba);
        _aoAtlas = new Texture2d(gl, atlasWidth, atlasHeight, aoData, InternalFormat.Rgba);
        _specularAtlas = new Texture2d(gl, atlasWidth, atlasHeight, specularData, InternalFormat.Rgba);
        _metallicAtlas = new Texture2d(gl, atlasWidth, atlasHeight, metallicData, InternalFormat.Rgba);
        _roughnessAtlas = new Texture2d(gl, atlasWidth, atlasHeight, roughnessData, InternalFormat.Rgba);
    }

    private static void CopyLayer(byte[] src, byte[] dst, int xOffset, int yOffset, int dstWidth, int srcWidth, int srcHeight)
    {
        for (var y = 0; y < srcHeight; y++)
        {
            for (var x = 0; x < srcWidth; x++)
            {
                var srcIdx = (y * srcWidth + x) * 4;
                var dstIdx = ((yOffset + y) * dstWidth + (xOffset + x)) * 4;

                if (srcIdx + 3 < src.Length && dstIdx + 3 < dst.Length)
                {
                    dst[dstIdx] = src[srcIdx];
                    dst[dstIdx + 1] = src[srcIdx + 1];
                    dst[dstIdx + 2] = src[srcIdx + 2];
                    dst[dstIdx + 3] = src[srcIdx + 3];
                }
            }
        }
    }

    private static void FillLayer(byte[] dst, int xOffset, int yOffset, int dstWidth, int tileW, int tileH, byte r, byte g, byte b, byte a)
    {
        for (var y = 0; y < tileH; y++)
        {
            for (var x = 0; x < tileW; x++)
            {
                var dstIdx = ((yOffset + y) * dstWidth + (xOffset + x)) * 4;
                if (dstIdx + 3 < dst.Length)
                {
                    dst[dstIdx] = r;
                    dst[dstIdx + 1] = g;
                    dst[dstIdx + 2] = b;
                    dst[dstIdx + 3] = a;
                }
            }
        }
    }

    public void Bind(TextureUnit diffuseUnit = TextureUnit.Texture0, 
                     TextureUnit normalUnit = TextureUnit.Texture1, 
                     TextureUnit aoUnit = TextureUnit.Texture2, 
                     TextureUnit specularUnit = TextureUnit.Texture3,
                     TextureUnit metallicUnit = TextureUnit.Texture4,
                     TextureUnit roughnessUnit = TextureUnit.Texture5)
    {
        _diffuseAtlas?.Bind(diffuseUnit);
        _normalAtlas?.Bind(normalUnit);
        _aoAtlas?.Bind(aoUnit);
        _specularAtlas?.Bind(specularUnit);
        _metallicAtlas?.Bind(metallicUnit);
        _roughnessAtlas?.Bind(roughnessUnit);
    }

    public bool TryGetUvs(ResourceLocation location, out (float U, float V, float Width, float Height) uv)
    {
        return _uvs.TryGetValue(location, out uv);
    }

    public void Dispose()
    {
        _diffuseAtlas?.Dispose();
        _normalAtlas?.Dispose();
        _aoAtlas?.Dispose();
        _specularAtlas?.Dispose();
        _metallicAtlas?.Dispose();
        _roughnessAtlas?.Dispose();
    }
}
