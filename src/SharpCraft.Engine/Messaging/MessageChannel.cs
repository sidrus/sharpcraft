using System.Collections.Concurrent;
using SharpCraft.Sdk.Messaging;

namespace SharpCraft.Engine.Messaging;

/// <summary>
/// Runtime implementation of a message channel.
/// </summary>
public class MessageChannel(string name) : IMessageChannel
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
                // This is a bit slow due to dynamic invocation, but flexible.
                // In a high-performance scenario, we'd want to optimize this.
                try
                {
                    var method = handler.GetType().GetMethod("Invoke");
                    method?.Invoke(handler, [message]);
                }
                catch (Exception)
                {
                    // Isolated exception as per specs 3.3
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
