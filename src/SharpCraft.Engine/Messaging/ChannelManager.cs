using Microsoft.Extensions.Logging;
using SharpCraft.Sdk.Messaging;
using System.Collections.Concurrent;

namespace SharpCraft.Engine.Messaging;

/// <summary>
/// Runtime implementation of the channel manager.
/// </summary>
public class ChannelManager(ILoggerFactory loggerFactory) : IChannelManager
{
    private readonly ConcurrentDictionary<string, MessageChannel> _channels = new();

    public IMessageChannel GetChannel(string name)
    {
        return _channels.GetOrAdd(name, n => new MessageChannel(n, loggerFactory.CreateLogger<MessageChannel>()));
    }
}