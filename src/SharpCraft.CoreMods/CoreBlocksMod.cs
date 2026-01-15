using SharpCraft.Sdk;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Sdk.Resources;
using StbImageSharp;

namespace SharpCraft.CoreMods;

/// <summary>
/// Core mod that registers the default vanilla blocks.
/// </summary>
public class CoreBlocksMod(ISharpCraftSdk sdk) : IMod
{
    private const string Namespace = "sharpcraft";

    public ModManifest Manifest => new(
        Id: Namespace,
        Name: "Core Blocks",
        Author: "Ejafi Software",
        Version: "1.0.0",
        Dependencies: [],
        Capabilities: ["blocks"],
        Entrypoints: ["SharpCraft.CoreMods.dll"]
    );

    public string BaseDirectory { get; set; } = string.Empty;

    public void OnEnable()
    {
        LoadTextures();
        RegisterDefaultBlocks();
    }

    public void OnDisable()
    {
        // No cleanup needed
    }

    private void LoadTextures()
    {
        var assetsDir = Path.Combine(BaseDirectory, "Assets", "Textures");
        var terrainPath = Path.Combine(assetsDir, "terrain.png");
        var normalPath = Path.Combine(assetsDir, "normals.png");
        var aoPath = Path.Combine(assetsDir, "ao.png");
        var specularPath = Path.Combine(assetsDir, "specular.png");

        if (File.Exists(terrainPath))
        {
            var terrainImg = LoadImage(terrainPath);
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

            foreach (var kvp in textureMapping)
            {
                var name = kvp.Key;
                var tileIndex = kvp.Value;

                var tx = tileIndex % terrainAtlasSize;
                var ty = tileIndex / terrainAtlasSize;

                var tileData = ExtractTile(terrainImg, tx, ty, tileW, tileH);
                var normalData = normalImg != null ? ExtractTile(normalImg, tx, ty, tileW, tileH) : null;
                var aoData = aoImg != null ? ExtractTile(aoImg, tx, ty, tileW, tileH) : null;
                var specularData = specularImg != null ? ExtractTile(specularImg, tx, ty, tileW, tileH) : null;

                sdk.Assets.Register($"{Namespace}:{name}", new TextureData(tileW, tileH, tileData, normalData, aoData, specularData));
            }
        }
        else
        {
            var textureNames = new[] { "grass_top", "grass_side", "dirt", "stone", "sand", "water", "bedrock" };
            foreach (var name in textureNames)
            {
                var data = new byte[16 * 16 * 4];
                for (var i = 0; i < data.Length; i += 4) { data[i] = 255; data[i + 1] = 0; data[i + 2] = 255; data[i + 3] = 255; }
                sdk.Assets.Register($"{Namespace}:{name}", new TextureData(16, 16, data));
            }
        }
    }

    private ImageResult LoadImage(string path)
    {
        using var stream = File.OpenRead(path);
        return ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
    }

    private byte[] ExtractTile(ImageResult image, int tx, int ty, int tileW, int tileH)
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

    private void RegisterDefaultBlocks()
    {
        // Define default blocks
        var blocks = new[]
        {
            new BlockDefinition(
                "sharpcraft:grass",
                "Grass",
                Type: BlockType.Grass,
                TextureTop: new ResourceLocation(Namespace, "grass_top"),
                TextureBottom: new ResourceLocation(Namespace, "dirt"),
                TextureSides: new ResourceLocation(Namespace, "grass_side")
            ),
            new BlockDefinition(
                "sharpcraft:dirt",
                "Dirt",
                Type: BlockType.Dirt,
                TextureSides: new ResourceLocation(Namespace, "dirt")
            ),
            new BlockDefinition(
                "sharpcraft:stone",
                "Stone",
                Type: BlockType.Stone,
                TextureSides: new ResourceLocation(Namespace, "stone")
            ),
            new BlockDefinition(
                "sharpcraft:sand",
                "Sand",
                Type: BlockType.Sand,
                TextureSides: new ResourceLocation(Namespace, "sand")
            ),
            new BlockDefinition(
                "sharpcraft:water",
                "Water",
                Type: BlockType.Water,
                IsSolid: false,
                IsTransparent: true,
                TextureSides: new ResourceLocation(Namespace, "water")
            ),
            new BlockDefinition(
                "sharpcraft:bedrock",
                "Bedrock",
                Type: BlockType.Bedrock,
                TextureSides: new ResourceLocation(Namespace, "bedrock")
            )
        };

        foreach (var def in blocks)
        {
            sdk.Blocks.Register(def.Id, def);
        }
    }
}
