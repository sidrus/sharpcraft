using SharpCraft.CoreMods.Blocks;
using SharpCraft.CoreMods.Rendering;
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

        var textureData = TextureLoader.LoadTextures(Namespace, terrainPath, normalPath, aoPath, specularPath);

        foreach (var (name, data) in textureData)
        {
            var rl = new ResourceLocation(Namespace, name);
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
