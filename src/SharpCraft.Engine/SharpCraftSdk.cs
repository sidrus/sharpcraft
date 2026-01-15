using SharpCraft.Sdk;
using SharpCraft.Sdk.Assets;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Messaging;
using SharpCraft.Sdk.Commands;
using SharpCraft.Sdk.UI;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.Engine;

/// <summary>
/// Runtime implementation of the SharpCraft SDK.
/// </summary>
public class SharpCraftSdk(
    IAssetRegistry assets,
    IBlockRegistry blocks,
    IChannelManager channels,
    ICommandRegistry commands,
    IWorldGenerationRegistry world,
    IHudRegistry huds)
    : ISharpCraftSdk
{
    public IAssetRegistry Assets { get; } = assets;
    public IBlockRegistry Blocks { get; } = blocks;
    public IChannelManager Channels { get; } = channels;
    public ICommandRegistry Commands { get; } = commands;
    public IWorldGenerationRegistry World { get; } = world;
    public IHudRegistry Huds { get; } = huds;
}
