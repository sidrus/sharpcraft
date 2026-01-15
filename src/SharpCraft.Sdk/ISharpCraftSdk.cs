using SharpCraft.Sdk.Assets;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Messaging;
using SharpCraft.Sdk.Commands;
using SharpCraft.Sdk.UI;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.Sdk;

/// <summary>
/// The main entry point for interacting with the SharpCraft SDK.
/// </summary>
public interface ISharpCraftSdk
{
    /// <summary>
    /// Gets the asset registry.
    /// </summary>
    IAssetRegistry Assets { get; }

    /// <summary>
    /// Gets the block registry.
    /// </summary>
    IBlockRegistry Blocks { get; }

    /// <summary>
    /// Gets the channel manager for pub/sub communication.
    /// </summary>
    IChannelManager Channels { get; }

    /// <summary>
    /// Gets the command registry.
    /// </summary>
    ICommandRegistry Commands { get; }

    /// <summary>
    /// Gets the world and terrain generation registry.
    /// </summary>
    IWorldGenerationRegistry World { get; }

    /// <summary>
    /// Gets the HUD registry.
    /// </summary>
    IHudRegistry Huds { get; }
}
