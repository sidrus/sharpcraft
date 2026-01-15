using SharpCraft.Sdk.Resources;
using StbImageSharp;

namespace SharpCraft.CoreMods.Rendering;

public static class TextureLoader
{
    public static IEnumerable<(string name, TextureData data)> LoadTextures(string ns, string albedoPath, string? normalPath, string? aoPath, string? specularPath)
    {
        if (File.Exists(albedoPath))
        {
            var terrainImg = LoadImage(albedoPath);
            var normalImg = File.Exists(normalPath) ? LoadImage(normalPath) : null;
            var aoImg = File.Exists(aoPath) ? LoadImage(aoPath) : null;
            var specularImg = File.Exists(specularPath) ? LoadImage(specularPath) : null;

            var textureMapping = new Dictionary<string, int>
            {
                { "grass_top", 0 },
                { "stone", 1 },
                { "dirt", 2 },
                { "grass_side", 3 },
                { "bedrock", 17 },
                { "sand", 18 },
                { "water", 19 }
            };

            const int terrainAtlasSize = 16;
            var tileW = terrainImg.Width / terrainAtlasSize;
            var tileH = terrainImg.Height / terrainAtlasSize;

            foreach (var (name, tileIndex) in textureMapping)
            {
                var tx = tileIndex % terrainAtlasSize;
                var ty = tileIndex / terrainAtlasSize;

                var tileData = ExtractTile(terrainImg, tx, ty, tileW, tileH);
                var normalData = normalImg != null ? ExtractTile(normalImg, tx, ty, tileW, tileH) : null;
                var aoData = aoImg != null ? ExtractTile(aoImg, tx, ty, tileW, tileH) : null;
                var specularData = specularImg != null ? ExtractTile(specularImg, tx, ty, tileW, tileH) : null;

                yield return (name, new TextureData(tileW, tileH, tileData, normalData, aoData, specularData));
            }
        }
        else
        {
            var textureNames = new[] { "grass_top", "grass_side", "dirt", "stone", "sand", "water", "bedrock" };
            foreach (var name in textureNames)
            {
                var data = new byte[16 * 16 * 4];
                for (var i = 0; i < data.Length; i += 4) { data[i] = 255; data[i + 1] = 0; data[i + 2] = 255; data[i + 3] = 255; }

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