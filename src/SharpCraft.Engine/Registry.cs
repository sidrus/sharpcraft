using System.Collections.Concurrent;
using SharpCraft.Sdk;

using SharpCraft.Sdk.Resources;

namespace SharpCraft.Engine;

/// <summary>
/// Base implementation of a registry.
/// </summary>
/// <typeparam name="T">The type of the object being registered.</typeparam>
public class Registry<T> : IRegistry<T>
{
    private readonly ConcurrentDictionary<ResourceLocation, T> _items = new();

    public int Count => _items.Count;

    public virtual void Register(ResourceLocation id, T item)
    {
        if (!_items.TryAdd(id, item))
        {
            throw new ArgumentException($"Item with ID '{id}' is already registered.", nameof(id));
        }
    }

    public T Get(ResourceLocation id)
    {
        if (_items.TryGetValue(id, out var item))
        {
            return item;
        }

        throw new KeyNotFoundException($"Item with ID '{id}' was not found.");
    }

    public bool TryGet(ResourceLocation id, out T? item)
    {
        return _items.TryGetValue(id, out item);
    }

    public IEnumerable<KeyValuePair<ResourceLocation, T>> All => _items;
}
