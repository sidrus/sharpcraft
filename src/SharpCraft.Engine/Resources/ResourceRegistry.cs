using SharpCraft.Sdk.Resources;

namespace SharpCraft.Engine.Resources;

/// <summary>
/// A registry that enforces namespaced resource locations.
/// </summary>
/// <typeparam name="T">The type of the resource.</typeparam>
public class ResourceRegistry<T> : Registry<T>;
