using SharpCraft.Sdk;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Sdk.Resources;

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

    public void OnEnable()
    {
        RegisterDefaultBlocks();
    }

    public void OnDisable()
    {
        // No cleanup needed
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
