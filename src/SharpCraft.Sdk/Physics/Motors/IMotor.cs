namespace SharpCraft.Sdk.Physics.Motors;

/// <summary>
/// Defines a component that applies forces to a physics entity based on movement intent.
/// </summary>
public interface IMotor
{
    /// <summary>
    /// Applies movement forces to the specified entity.
    /// </summary>
    /// <param name="entity">The physics entity to apply forces to.</param>
    /// <param name="intent">The desired movement direction and actions.</param>
    /// <param name="deltaTime">The elapsed time since the last update.</param>
    void ApplyForces(IPhysicsEntity entity, MovementIntent intent, float deltaTime);
}