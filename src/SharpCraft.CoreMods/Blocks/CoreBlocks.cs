using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Resources;

namespace SharpCraft.CoreMods.Blocks;

public static class CoreBlocks
{
    private static readonly FluidProperties WaterFluid = new(
        Density: 1000f,
        Friction: 0.15f,
        BuoyantGravity: -2.0f,
        SwimUpVelocity: 2.0f,
        DeepSwimUpVelocity: 4.0f,
        DeepSwimDepth: 0.8f,
        SwimDownVelocity: -2.0f,
        SpeedMultiplier: 0.5f);

    public static readonly BlockDefinition[] Definitions =
    [
        new(
            "sharpcraft:grass",
            "Grass",
            Friction: 0.5f,
            TextureTop: new ResourceLocation(CoreMod.Namespace, "grass_top"),
            TextureBottom: new ResourceLocation(CoreMod.Namespace, "dirt"),
            TextureSides: new ResourceLocation(CoreMod.Namespace, "grass_side")
        ),
        new(
            "sharpcraft:dirt",
            "Dirt",
            Friction: 0.5f,
            TextureSides: new ResourceLocation(CoreMod.Namespace, "dirt")
        ),
        new(
            "sharpcraft:stone",
            "Stone",
            Friction: 0.5f,
            TextureSides: new ResourceLocation(CoreMod.Namespace, "stone")
        ),
        new(
            "sharpcraft:sand",
            "Sand",
            TextureSides: new ResourceLocation(CoreMod.Namespace, "sand")
        ),
        new(
            "sharpcraft:water",
            "Water",
            Flags: BlockFlags.Transparent | BlockFlags.Fluid,
            Fluid: WaterFluid,
            TextureSides: new ResourceLocation(CoreMod.Namespace, "water")
        ),
        new(
            "sharpcraft:bedrock",
            "Bedrock",
            TextureSides: new ResourceLocation(CoreMod.Namespace, "bedrock")
        )
    ];
}