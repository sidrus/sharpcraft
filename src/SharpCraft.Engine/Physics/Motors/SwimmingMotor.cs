using SharpCraft.Sdk.Physics;

namespace SharpCraft.Engine.Physics.Motors;

/// <summary>
/// Drives the player through water: buoyancy, swimming up/down, treading at the surface,
/// and hopping out onto adjacent land.
/// </summary>
public sealed class SwimmingMotor : PlayerMotorBase
{
    private const float SwimSpeedMultiplier = 0.5f;

    /// <summary>Baseline upward swim speed while submerged, in m/s.</summary>
    private const float SwimUpVelocity = 2.0f;

    /// <summary>Stronger upward kick when fully submerged and deep enough, in m/s.</summary>
    private const float DeepSwimUpVelocity = 4.0f;

    /// <summary>Submersion depth past which the stronger kick applies.</summary>
    private const float DeepSwimDepth = 0.8f;

    /// <summary>Downward swim speed when descending, in m/s.</summary>
    private const float SwimDownVelocity = -2.0f;

    /// <inheritdoc />
    public override void ApplyForces(IPhysicsEntity entity, MovementIntent intent, float deltaTime)
    {
        Friction = PhysicsConstants.WaterFriction;
        var speed = GetBaseSpeed(intent) * SwimSpeedMultiplier;

        var onSurface = SensorData?.IsOnWaterSurface ?? false;
        var nextToLedge = SensorData?.IsNextToClimbableLedge ?? false;
        var depth = SensorData?.SubmersionDepth ?? 0f;
        var isClimbingOut = onSurface && nextToLedge && intent.IsJumping;

        var velocity = entity.Velocity;

        if (isClimbingOut)
        {
            // Next to a ledge: give a real jump impulse to climb out onto land.
            velocity.Y = Math.Max(velocity.Y, WalkingMotor.JumpVelocity);
        }
        else if (intent.IsJumping && !onSurface)
        {
            // Submerged and ascending: swim toward the surface.
            velocity.Y = Math.Max(velocity.Y, SwimUpVelocity);
            if (depth > DeepSwimDepth)
                velocity.Y = Math.Max(velocity.Y, DeepSwimUpVelocity);
        }
        else if (intent.IsDescending)
        {
            velocity.Y = Math.Min(velocity.Y, SwimDownVelocity);
        }

        // Reduced gravity (buoyancy) in water.
        var gravity = PhysicsConstants.WaterGravity;
        var terminalVelocity = ComputeTerminalVelocity(gravity, PhysicsConstants.WaterDensity);
        velocity.Y += gravity * deltaTime;
        if (velocity.Y < terminalVelocity)
            velocity.Y = terminalVelocity;

        // Surface tread: at the waterline the player floats but cannot rise above it.
        // This is what prevents "walking on water" by holding or spamming jump — the only
        // way to gain height at the surface is the ledge hop handled above.
        if (onSurface && !isClimbingOut && velocity.Y > 0f)
            velocity.Y = 0f;

        entity.Velocity = velocity;

        ApplyHorizontalMovement(entity, intent, speed, deltaTime);
    }
}
