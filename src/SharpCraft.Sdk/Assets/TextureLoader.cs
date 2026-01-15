using SharpCraft.Sdk.Resources;
using StbImageSharp;

namespace SharpCraft.Sdk.Assets;

/// <summary>
/// Utility class for loading texture data from image files.
/// </summary>
public static class TextureLoader
{
    /// <summary>
    /// Loads textures from an atlas image based on a mapping of names to tile indices.
    /// </summary>
    /// <param name="albedoPath">Path to the main atlas image.</param>
    /// <param name="textureMapping">A dictionary mapping texture names to their index in the atlas.</param>
    /// <param name="normalPath">Optional path to the normal map atlas.</param>
    /// <param name="aoPath">Optional path to the ambient occlusion map atlas.</param>
    /// <param name="specularPath">Optional path to the specular map atlas.</param>
    /// <param name="metallicPath">Optional path to the metallic map atlas.</param>
    /// <param name="roughnessPath">Optional path to the roughness map atlas.</param>
    /// <param name="atlasSize">The number of tiles along one side of the atlas (defaults to 16).</param>
    /// <returns>An enumerable of texture names and their corresponding data.</returns>
    public static IEnumerable<(string name, TextureData data)> LoadTexturesFromAtlas(
        string albedoPath,
        IReadOnlyDictionary<string, int> textureMapping,
        string? normalPath = null,
        string? aoPath = null,
        string? specularPath = null,
        string? metallicPath = null,
        string? roughnessPath = null,
        int atlasSize = 16)
    {
        if (File.Exists(albedoPath))
        {
            var terrainImg = LoadImage(albedoPath);
            var normalImg = !string.IsNullOrEmpty(normalPath) && File.Exists(normalPath) ? LoadImage(normalPath) : null;
            var aoImg = !string.IsNullOrEmpty(aoPath) && File.Exists(aoPath) ? LoadImage(aoPath) : null;
            var specularImg = !string.IsNullOrEmpty(specularPath) && File.Exists(specularPath) ? LoadImage(specularPath) : null;
            var metallicImg = !string.IsNullOrEmpty(metallicPath) && File.Exists(metallicPath) ? LoadImage(metallicPath) : null;
            var roughnessImg = !string.IsNullOrEmpty(roughnessPath) && File.Exists(roughnessPath) ? LoadImage(roughnessPath) : null;

            var tileW = terrainImg.Width / atlasSize;
            var tileH = terrainImg.Height / atlasSize;

            foreach (var (name, tileIndex) in textureMapping)
            {
                var tx = tileIndex % atlasSize;
                var ty = tileIndex / atlasSize;

                var tileData = ExtractTile(terrainImg, tx, ty, tileW, tileH);
                var normalData = normalImg != null ? ExtractTile(normalImg, tx, ty, tileW, tileH) : null;
                var aoData = aoImg != null ? ExtractTile(aoImg, tx, ty, tileW, tileH) : null;
                var specularData = specularImg != null ? ExtractTile(specularImg, tx, ty, tileW, tileH) : null;
                var metallicData = metallicImg != null ? ExtractTile(metallicImg, tx, ty, tileW, tileH) : null;
                var roughnessData = roughnessImg != null ? ExtractTile(roughnessImg, tx, ty, tileW, tileH) : null;

                yield return (name, new TextureData(tileW, tileH, tileData, normalData, aoData, specularData, metallicData, roughnessData));
            }
        }
        else
        {
            foreach (var name in textureMapping.Keys)
            {
                var data = new byte[16 * 16 * 4];
                for (var i = 0; i < data.Length; i += 4)
                {
                    data[i] = 255;
                    data[i + 1] = 0;
                    data[i + 2] = 255;
                    data[i + 3] = 255;
                }

                yield return (name, new TextureData(16, 16, data));
            }
        }
    }

    private static ImageResult LoadImage(string path)
    {
        using var stream = File.OpenRead(path);
        return ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
    }

    private static byte[] ExtractTile(ImageResult image, int tx, int ty, int tileW, int tileH)
    {
        var tileData = new byte[tileW * tileH * 4];
        for (var y = 0; y < tileH; y++)
        {
            for (var x = 0; x < tileW; x++)
            {
                var srcIdx = ((ty * tileH + y) * image.Width + (tx * tileW + x)) * 4;
                var dstIdx = (y * tileW + x) * 4;

                tileData[dstIdx] = image.Data[srcIdx];
                tileData[dstIdx + 1] = image.Data[srcIdx + 1];
                tileData[dstIdx + 2] = image.Data[srcIdx + 2];
                tileData[dstIdx + 3] = image.Data[srcIdx + 3];
            }
        }

        return tileData;
    }
}
