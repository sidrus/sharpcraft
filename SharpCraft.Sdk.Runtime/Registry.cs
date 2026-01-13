using System.Collections.Concurrent;
using SharpCraft.Sdk;

namespace SharpCraft.Sdk.Runtime;

/// <summary>
/// Base implementation of a registry.
/// </summary>
/// <typeparam name="T">The type of the object being registered.</typeparam>
public class Registry<T> : IRegistry<T>
{
    private readonly ConcurrentDictionary<string, T> _items = new();

    public virtual void Register(string id, T item)
    {
        if (!_items.TryAdd(id, item))
        {
            throw new ArgumentException($"Item with ID '{id}' is already registered.", nameof(id));
        }
    }

    public T Get(string id)
    {
        if (_items.TryGetValue(id, out var item))
        {
            return item;
        }

        throw new KeyNotFoundException($"Item with ID '{id}' was not found.");
    }

    public bool TryGet(string id, out T item)
    {
        return _items.TryGetValue(id, out item!);
    }

    public IEnumerable<KeyValuePair<string, T>> All => _items;
}
