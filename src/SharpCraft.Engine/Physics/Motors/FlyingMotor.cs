using SharpCraft.Sdk.Physics;

namespace SharpCraft.Engine.Physics.Motors;

/// <summary>
/// Drives the player in free-flight: no gravity, with smoothed vertical control from the
/// jump (ascend) and descend inputs.
/// </summary>
public sealed class FlyingMotor : PlayerMotorBase
{
    private const float FlySpeedMultiplier = 2.5f;
    private const float VerticalResponse = 10f;

    /// <inheritdoc />
    public override void ApplyForces(IPhysicsEntity entity, MovementIntent intent, float deltaTime)
    {
        Friction = PhysicsConstants.FlyingFriction;
        var speed = GetBaseSpeed(intent) * FlySpeedMultiplier;

        var velocity = entity.Velocity;
        if (intent.IsJumping)
        {
            velocity.Y = float.Lerp(velocity.Y, speed, deltaTime * VerticalResponse);
        }
        else if (intent.IsDescending)
        {
            velocity.Y = float.Lerp(velocity.Y, -speed, deltaTime * VerticalResponse);
        }
        else
        {
            velocity.Y = float.Lerp(velocity.Y, 0, deltaTime * VerticalResponse);
        }

        entity.Velocity = velocity;

        ApplyHorizontalMovement(entity, intent, speed, deltaTime);
    }
}