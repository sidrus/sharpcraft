using SharpCraft.CoreMods.Blocks;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Assets;
using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Sdk.Rendering;
using SharpCraft.Sdk.Resources;

namespace SharpCraft.CoreMods;

/// <summary>
/// Core mod that registers the default vanilla blocks.
/// </summary>
public class CoreMod(ISharpCraftSdk sdk) : IMod
{
    internal const string Namespace = "sharpcraft";

    public ModManifest Manifest => new(
        Id: Namespace,
        Name: "Core",
        Author: "Ejafi Software",
        Version: "0.0.1",
        Dependencies: [],
        Capabilities: ["blocks", "world-gen"],
        Entrypoints: ["SharpCraft.CoreMods.dll"]
    );

    public string BaseDirectory { get; set; } = string.Empty;

    public void OnEnable()
    {
        LoadTextures();
        RegisterDefaultBlocks();
        RegisterWorldGenerators();
        RegisterHuds();
        RegisterCommands();
    }

    private void RegisterCommands()
    {
        Commands.DefaultCommands.Register(sdk);
    }

    private void RegisterHuds()
    {
        sdk.Huds.RegisterHud(new UI.MainHud());
        sdk.Huds.RegisterHud(new UI.DebugHud());
        sdk.Huds.RegisterHud(new UI.GraphicsSettingsHud());
        sdk.Huds.RegisterHud(new UI.DeveloperHud());
    }

    public void OnDisable()
    {
        // No cleanup needed
    }

    private void RegisterWorldGenerators()
    {
        sdk.World.Register(new ResourceLocation(Namespace, "default"), new Universe.DefaultWorldGenerator());
    }

    private void LoadTextures()
    {
        var assetsDir = Path.Combine(BaseDirectory, "Assets", "Textures");
        var material = new Material(Path.Combine(assetsDir, "terrain.png"))
        {
            NormalPath = Path.Combine(assetsDir, "normals.png"),
            AmbientOcclusionPath = Path.Combine(assetsDir, "ao.png"),
            MetallicPath = Path.Combine(assetsDir, "metallic.png"),
            RoughnessPath = Path.Combine(assetsDir, "roughness.png")
        };

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

        var textureData = TextureLoader.LoadTexturesFromAtlas(material, textureMapping);

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
