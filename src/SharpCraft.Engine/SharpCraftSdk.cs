using SharpCraft.Sdk;
using SharpCraft.Sdk.Assets;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Resources;
using SharpCraft.Sdk.Messaging;
using SharpCraft.Sdk.Commands;
using SharpCraft.Sdk.Rendering;
using SharpCraft.Sdk.UI;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.Engine;

/// <summary>
/// Runtime implementation of the SharpCraft SDK.
/// </summary>
public class SharpCraftSdk(
    IRegistry<TextureData> assets,
    IBlockRegistry blocks,
    IChannelManager channels,
    ICommandRegistry commands,
    IRegistry<IWorldGenerator> world,
    IHudRegistry huds,
    ILightingSystem lighting)
    : ISharpCraftSdk
{
    public IRegistry<TextureData> Assets { get; } = assets;
    public IBlockRegistry Blocks { get; } = blocks;
    public IChannelManager Channels { get; } = channels;
    public ICommandRegistry Commands { get; } = commands;
    public IRegistry<IWorldGenerator> World { get; } = world;
    public IHudRegistry Huds { get; } = huds;
    public ILightingSystem Lighting { get; } = lighting;
}
