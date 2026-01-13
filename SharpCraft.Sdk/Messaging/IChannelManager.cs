namespace SharpCraft.Sdk.Messaging;

/// <summary>
/// Manages message channels for pub/sub communication.
/// </summary>
public interface IChannelManager
{
    /// <summary>
    /// Gets or creates a message channel with the given name.
    /// </summary>
    /// <param name="name">The name of the channel (e.g., "mod://economy/transactions").</param>
    /// <returns>The message channel.</returns>
    IMessageChannel GetChannel(string name);
}
