using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Sdk.Physics;

namespace SharpCraft.Sdk;

/// <summary>
/// Represents an entity in the game world with a transform and lifecycle.
/// </summary>
public interface IActor : ILifecycle, IComponentProvider
{
    /// <summary>
    /// Gets the current transform (position, rotation, scale) of the actor.
    /// </summary>
    public Transform Transform { get; }

    /// <summary>
    /// Gets the display name of the actor.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the unique identifier of the actor.
    /// </summary>
    public string Id { get; }
}