using Microsoft.Extensions.Logging;
using SharpCraft.Sdk.Messaging;
using System.Collections.Concurrent;

namespace SharpCraft.Engine.Messaging;

/// <summary>
/// Runtime implementation of a message channel.
/// </summary>
public partial class MessageChannel(string name, ILogger<MessageChannel> logger) : IMessageChannel
{
    private readonly ConcurrentDictionary<Type, List<object>> _handlers = new();

    public string Name { get; } = name;

    public void Publish(object message)
    {
        var type = message.GetType();
        if (_handlers.TryGetValue(type, out var handlers))
        {
            List<object> handlersCopy;
            lock (handlers)
            {
                handlersCopy = new List<object>(handlers);
            }

            foreach (var handler in handlersCopy)
            {
                try
                {
                    ((Delegate)handler).DynamicInvoke(message);
                }
                catch (Exception e)
                {
                    LogHandlerFailed(Name, type, e);
                }
            }
        }
    }

    public IDisposable Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        var handlers = _handlers.GetOrAdd(type, _ => []);

        lock (handlers)
        {
            handlers.Add(handler);
        }

        return new Unsubscriber(handlers, handler);
    }

    private sealed class Unsubscriber(List<object> handlers, object handler) : IDisposable
    {
        public void Dispose()
        {
            lock (handlers)
            {
                handlers.Remove(handler);
            }
        }
    }
}