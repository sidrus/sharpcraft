namespace SharpCraft.Sdk.Messaging;

/// <summary>
/// Defines a message channel for pub/sub communication.
/// </summary>
public interface IMessageChannel
{
    /// <summary>
    /// Gets the name of the channel.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Publishes a message to the channel.
    /// </summary>
    /// <param name="message">The message object.</param>
    void Publish(object message);

    /// <summary>
    /// Subscribes to messages of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of messages to subscribe to.</typeparam>
    /// <param name="handler">The message handler.</param>
    /// <returns>An IDisposable that can be used to unsubscribe.</returns>
    IDisposable Subscribe<T>(Action<T> handler);
}
