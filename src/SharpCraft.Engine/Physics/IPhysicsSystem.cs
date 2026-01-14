using System.Numerics;

namespace SharpCraft.Engine.Physics;

/// <summary>
/// Defines a system for handling physics movement and collision resolution.
/// </summary>
public interface IPhysicsSystem
{
    /// <summary>
    /// Moves an entity and resolves any collisions that occur along the way.
    /// </summary>
    /// <param name="position">The current position.</param>
    /// <param name="velocity">The velocity to apply.</param>
    /// <param name="size">The size of the entity's bounding box.</param>
    /// <returns>The new position after movement and resolution.</returns>
    public Vector3 MoveAndResolve(Vector3 position, Vector3 velocity, Vector3 size);
}
