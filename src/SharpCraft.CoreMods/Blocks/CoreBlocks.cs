using System.Collections.Immutable;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Resources;

namespace SharpCraft.CoreMods.Blocks;

public static class CoreBlocks
{
    public static readonly BlockDefinition[] Definitions =
    [
        new(
            BlockIds.Air,
            "Air",
            IsSolid: false,
            IsTransparent: true
        ),
        new(
            BlockIds.Grass,
            "Grass",
            TextureTop: new ResourceLocation(CoreBlocksMod.Namespace, "textures/block/grass_top"),
            TextureBottom: new ResourceLocation(CoreBlocksMod.Namespace, "textures/block/dirt"),
            TextureSides: new ResourceLocation(CoreBlocksMod.Namespace, "textures/block/grass_side")
        ),
        new(
            BlockIds.Dirt,
            "Dirt",
            TextureSides: new ResourceLocation(CoreBlocksMod.Namespace, "textures/block/dirt")
        ),
        new(
            BlockIds.Stone,
            "Stone",
            TextureSides: new ResourceLocation(CoreBlocksMod.Namespace, "textures/block/stone")
        ),
        new(
            BlockIds.Sand,
            "Sand",
            TextureSides: new ResourceLocation(CoreBlocksMod.Namespace, "textures/block/sand")
        ),
        new(
            BlockIds.Water,
            "Water",
            IsSolid: false,
            IsTransparent: true,
            TextureSides: new ResourceLocation(CoreBlocksMod.Namespace, "textures/block/water")
        ),
        new(
            BlockIds.Bedrock,
            "Bedrock",
            TextureSides: new ResourceLocation(CoreBlocksMod.Namespace, "textures/block/bedrock")
        )
    ];
}