using SharpCraft.Sdk;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Commands;
using SharpCraft.Sdk.Messaging;
using SharpCraft.Sdk.Rendering;
using SharpCraft.Sdk.Resources;
using SharpCraft.Sdk.UI;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.Engine;

/// <summary>
/// Runtime implementation of the SharpCraft SDK.
/// </summary>
public class SharpCraftSdk : ISharpCraftSdk
{
    public required IRegistry<TextureData> Assets
    {
        get; init;
    }
    public required IBlockRegistry Blocks
    {
        get; init;
    }
    public required IChannelManager Channels
    {
        get; init;
    }
    public required ICommandRegistry Commands
    {
        get; init;
    }
    public required IRegistry<IWorldGenerator> World
    {
        get; init;
    }
    public required IHudRegistry Huds
    {
        get; init;
    }
    public required ILightingSystem Lighting
    {
        get; init;
    }
    public required IGraphicsSettings GraphicsSettings
    {
        get; init;
    }
}