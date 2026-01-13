using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Messaging;
using SharpCraft.Sdk.Commands;
using SharpCraft.Sdk.World;

namespace SharpCraft.Sdk.Runtime;

/// <summary>
/// Runtime implementation of the SharpCraft SDK.
/// </summary>
public class SharpCraftSdk(
    IBlockRegistry blocks,
    IChannelManager channels,
    ICommandRegistry commands,
    IWorldGenerationRegistry world)
    : ISharpCraftSdk
{
    public IBlockRegistry Blocks { get; } = blocks;
    public IChannelManager Channels { get; } = channels;
    public ICommandRegistry Commands { get; } = commands;
    public IWorldGenerationRegistry World { get; } = world;
}
