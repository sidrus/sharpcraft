using SharpCraft.Sdk.Physics;

namespace SharpCraft.Engine.Physics.Motors;

/// <summary>
/// Drives the player on land and through the air: ground jumping, gravity and
/// friction from the block underfoot.
/// </summary>
public sealed class WalkingMotor : PlayerMotorBase
{
    /// <summary>The upward velocity applied by a ground jump, in m/s.</summary>
    public const float JumpVelocity = 5.0f;

    /// <inheritdoc />
    public override void ApplyForces(IPhysicsEntity entity, MovementIntent intent, float deltaTime)
    {
        var isGrounded = SensorData?.IsGrounded ?? entity.IsGrounded;
        var blockBelow = SensorData?.BlockBelow ?? default;
        var canJump = isGrounded && blockBelow.IsSolid;

        Friction = canJump ? (Material?.GroundFriction ?? PhysicsConstants.AirFriction) : PhysicsConstants.AirFriction;

        var speed = GetBaseSpeed(intent);
        var gravity = PhysicsConstants.DefaultGravity;
        var terminalVelocity = ComputeTerminalVelocity(gravity, PhysicsConstants.AirDensity);

        var velocity = entity.Velocity;
        if (intent.IsJumping && canJump)
        {
            velocity.Y = JumpVelocity;
        }

        entity.Velocity = velocity;

        ApplyHorizontalMovement(entity, intent, speed, deltaTime);

        velocity = entity.Velocity;
        velocity.Y = ApplyGravity(velocity.Y, gravity, terminalVelocity, deltaTime);

        entity.Velocity = velocity;
    }
}