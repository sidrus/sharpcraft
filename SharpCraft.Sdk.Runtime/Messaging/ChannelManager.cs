using System.Collections.Concurrent;
using SharpCraft.Sdk.Messaging;

namespace SharpCraft.Sdk.Runtime.Messaging;

/// <summary>
/// Runtime implementation of the channel manager.
/// </summary>
public class ChannelManager : IChannelManager
{
    private readonly ConcurrentDictionary<string, MessageChannel> _channels = new();

    public IMessageChannel GetChannel(string name)
    {
        return _channels.GetOrAdd(name, n => new MessageChannel(n));
    }
}
