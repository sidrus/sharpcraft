using SharpCraft.Sdk.Resources;

namespace SharpCraft.Sdk.Blocks;

/// <summary>
/// Represents a single block in the world.
/// </summary>
public struct Block
{
    /// <summary>
    /// Gets or sets the unique identifier of the block type.
    /// </summary>
    public ResourceLocation Id { get; set; }

    /// <summary>
    /// Gets a value indicating whether this block is air.
    /// </summary>
    public bool IsAir => Id == null || Id == BlockIds.Air;
}

/// <summary>
/// Common block identifiers.
/// </summary>
public static class BlockIds
{
    private const string SharpcraftNamespace = "sharpcraft";
    
    public static readonly ResourceLocation Air = new(SharpcraftNamespace, "blocks/air/default");
    public static readonly ResourceLocation Grass = new(SharpcraftNamespace, "blocks/grass/default");
    public static readonly ResourceLocation Dirt = new(SharpcraftNamespace, "blocks/dirt/default");
    public static readonly ResourceLocation Stone = new(SharpcraftNamespace, "blocks/stone/default");
    public static readonly ResourceLocation Sand = new(SharpcraftNamespace, "blocks/sand/default");
    public static readonly ResourceLocation Water = new(SharpcraftNamespace, "blocks/water/default");
    public static readonly ResourceLocation Bedrock = new(SharpcraftNamespace, "blocks/bedrock/default");
    public static readonly ResourceLocation Lava = new(SharpcraftNamespace, "blocks/lava/default");
}
