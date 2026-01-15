namespace SharpCraft.Sdk;

/// <summary>
/// Provides a mechanism for actors to manage and retrieve components.
/// </summary>
public interface IComponentProvider
{
    /// <summary>
    /// Retrieves a component of the specified type from the provider.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <returns>The component instance if found; otherwise, <see langword="null"/>.</returns>
    public T? GetComponent<T>() where T : class;

    /// <summary>
    /// Adds a component of the specified type to the provider.
    /// </summary>
    /// <typeparam name="T">The type of component to add.</typeparam>
    /// <param name="component">The component instance to add.</param>
    public void AddComponent<T>(T component) where T : class;
}