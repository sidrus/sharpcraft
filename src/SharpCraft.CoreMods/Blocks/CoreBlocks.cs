using System.Collections.Immutable;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Resources;

namespace SharpCraft.CoreMods.Blocks;

public static class CoreBlocks
{
    public static readonly BlockDefinition[] Definitions =
    [
        new(
            "sharpcraft:grass",
            "Grass",
            Type: BlockType.Grass,
            TextureTop: new ResourceLocation(CoreBlocksMod.Namespace, "grass_top"),
            TextureBottom: new ResourceLocation(CoreBlocksMod.Namespace, "dirt"),
            TextureSides: new ResourceLocation(CoreBlocksMod.Namespace, "grass_side")
        ),
        new(
            "sharpcraft:dirt",
            "Dirt",
            Type: BlockType.Dirt,
            TextureSides: new ResourceLocation(CoreBlocksMod.Namespace, "dirt")
        ),
        new(
            "sharpcraft:stone",
            "Stone",
            Type: BlockType.Stone,
            TextureSides: new ResourceLocation(CoreBlocksMod.Namespace, "stone")
        ),
        new(
            "sharpcraft:sand",
            "Sand",
            Type: BlockType.Sand,
            TextureSides: new ResourceLocation(CoreBlocksMod.Namespace, "sand")
        ),
        new(
            "sharpcraft:water",
            "Water",
            Type: BlockType.Water,
            IsSolid: false,
            IsTransparent: true,
            TextureSides: new ResourceLocation(CoreBlocksMod.Namespace, "water")
        ),
        new(
            "sharpcraft:bedrock",
            "Bedrock",
            Type: BlockType.Bedrock,
            TextureSides: new ResourceLocation(CoreBlocksMod.Namespace, "bedrock")
        )
    ];
}