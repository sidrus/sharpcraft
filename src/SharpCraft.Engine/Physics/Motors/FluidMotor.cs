using SharpCraft.Sdk.Physics;

namespace SharpCraft.Engine.Physics.Motors;

/// <summary>
/// Drives the player through a fluid: buoyancy, swimming up/down, treading at the surface,
/// and hopping out onto adjacent land. All fluid behavior comes from the block's
/// data, so water, lava, etc. differ only by values.
/// </summary>
public sealed class FluidMotor : PlayerMotorBase
{
    /// <inheritdoc />
    public override void ApplyForces(IPhysicsEntity entity, MovementIntent intent, float deltaTime)
    {
        if (Material?.Fluid is not { } fluid)
        {
            return;
        }

        Friction = fluid.Friction;
        var speed = GetBaseSpeed(intent) * fluid.SpeedMultiplier;

        var onSurface = SensorData?.IsOnFluidSurface ?? false;
        var nextToLedge = SensorData?.IsNextToClimbableLedge ?? false;
        var depth = SensorData?.SubmersionDepth ?? 0f;
        var isClimbingOut = onSurface && nextToLedge && intent.IsJumping;

        var velocity = entity.Velocity;

        if (isClimbingOut)
        {
            velocity.Y = Math.Max(velocity.Y, WalkingMotor.JumpVelocity);
        }
        else if (intent.IsJumping && !onSurface)
        {
            velocity.Y = Math.Max(velocity.Y, fluid.SwimUpVelocity);
            if (depth > fluid.DeepSwimDepth)
            {
                velocity.Y = Math.Max(velocity.Y, fluid.DeepSwimUpVelocity);
            }
        }
        else if (intent.IsDescending)
        {
            velocity.Y = Math.Min(velocity.Y, fluid.SwimDownVelocity);
        }

        var gravity = fluid.BuoyantGravity;
        var terminalVelocity = ComputeTerminalVelocity(gravity, fluid.Density);
        velocity.Y = ApplyGravity(velocity.Y, gravity, terminalVelocity, deltaTime);

        if (onSurface && !isClimbingOut && velocity.Y > 0f)
        {
            velocity.Y = 0f;
        }

        entity.Velocity = velocity;

        ApplyHorizontalMovement(entity, intent, speed, deltaTime);
    }
}