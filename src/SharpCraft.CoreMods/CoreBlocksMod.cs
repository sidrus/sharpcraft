using SharpCraft.CoreMods.Blocks;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Assets;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Sdk.Resources;

namespace SharpCraft.CoreMods;

/// <summary>
/// Core mod that registers the default vanilla blocks.
/// </summary>
public class CoreBlocksMod(ISharpCraftSdk sdk) : IMod
{
    internal const string Namespace = "sharpcraft";

    public ModManifest Manifest => new(
        Id: Namespace,
        Name: "Core Blocks",
        Author: "Ejafi Software",
        Version: "0.0.1",
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

        var textureData = TextureLoader.LoadTexturesFromAtlas(terrainPath, textureMapping, normalPath, aoPath, specularPath);

        foreach (var (name, data) in textureData)
        {
            var rl = new ResourceLocation(Namespace, $"textures/block/{name}");
            sdk.Assets.Register(rl, data);
        }
    }

    private void RegisterDefaultBlocks()
    {
        foreach (var def in CoreBlocks.Definitions)
        {
            sdk.Blocks.Register(def.Id, def);
        }
    }
}
