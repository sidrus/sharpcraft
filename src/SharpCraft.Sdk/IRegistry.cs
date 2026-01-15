namespace SharpCraft.Sdk;

/// <summary>
/// Provides a versioned registry for engine objects.
/// </summary>
/// <typeparam name="T">The type of the object being registered.</typeparam>
public interface IRegistry<T>
{
    /// <summary>
    /// Registers an object with the given unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier (e.g., "mod:my_block").</param>
    /// <param name="item">The object to register.</param>
    void Register(string id, T item);

    /// <summary>
    /// Retrieves an object by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <returns>The registered object.</returns>
    T Get(string id);

    /// <summary>
    /// Attempts to retrieve an object by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="item">The registered object if found.</param>
    /// <returns>True if the object was found; otherwise, false.</returns>
    bool TryGet(string id, out T? item);

    /// <summary>
    /// Gets all registered items.
    /// </summary>
    IEnumerable<KeyValuePair<string, T>> All { get; }
}
