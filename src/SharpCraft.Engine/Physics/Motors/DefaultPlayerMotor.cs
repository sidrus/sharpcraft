using System.Numerics;
using SharpCraft.Sdk.Numerics;
using SharpCraft.Sdk.Physics;
using SharpCraft.Sdk.Physics.Motors;
using SharpCraft.Sdk.Physics.Sensors.Spatial;

namespace SharpCraft.Engine.Physics.Motors;

/// <summary>
/// A default implementation of a player motor that handles movement, jumping, and swimming.
/// </summary>
public class DefaultPlayerMotor : IMotor
{
    private const float WalkSpeed = 1.42f;
    private const float SprintSpeed = 3.84f;

    /// <summary>
    /// Gets or sets the current friction coefficient.
    /// </summary>
    public float Friction { get; private set; } = PhysicsConstants.AirFriction;

    /// <summary>
    /// Gets or sets the sensor data used for movement calculations.
    /// </summary>
    public SpatialSensorData? SensorData { get; set; }

    /// <inheritdoc />
    public void ApplyForces(IPhysicsEntity entity, MovementIntent intent, float deltaTime)
    {
        var (gravity, terminalVelocity, walkSpeed, canJump) = CalculatePhysicsState(entity, intent);

        HandleJumpAndVerticalMovement(entity, intent, deltaTime, walkSpeed, canJump);
        ApplyMovementInternal(entity, intent, gravity, terminalVelocity, walkSpeed, deltaTime);
    }

    private (float gravity, float terminalVelocity, float walkSpeed, bool canJump) CalculatePhysicsState(IPhysicsEntity entity, MovementIntent intent)
    {
        var isGrounded = SensorData?.IsGrounded ?? entity.IsGrounded;
        var blockBelow = SensorData?.BlockBelow ?? default;

        var canJump = isGrounded && blockBelow.IsSolid;

        var gravity = PhysicsConstants.DefaultGravity;
        var density = PhysicsConstants.AirDensity;
        var walkSpeed = intent.IsSprinting ? SprintSpeed : WalkSpeed;
        var friction = canJump ? blockBelow.Friction : PhysicsConstants.AirFriction;

        if (intent.IsFlying)
        {
            gravity = 0f;
            density = (SensorData?.IsSwimming ?? false) || (SensorData?.IsOnWaterSurface ?? false)
                ? PhysicsConstants.WaterDensity
                : PhysicsConstants.AirDensity;
            walkSpeed = WalkSpeed * 2.5f;
            friction = PhysicsConstants.FlyingFriction;
        }
        else if (SensorData?.IsSwimming ?? false)
        {
            gravity = PhysicsConstants.WaterGravity;
            density = PhysicsConstants.WaterDensity;
            walkSpeed = WalkSpeed * 0.5f;
            friction = PhysicsConstants.WaterFriction;
        }
        else if (SensorData?.IsOnWaterSurface ?? false)
        {
            gravity = PhysicsConstants.DefaultGravity;
            density = PhysicsConstants.AirDensity;
            walkSpeed = WalkSpeed * 0.8f;
            friction = PhysicsConstants.WaterFriction;
        }

        Friction = friction;

        var terminalVelocity = intent.IsFlying
            ? -100f
            : -PhysicsConstants.CalculateTerminalVelocity(
                PhysicsConstants.DefaultMass,
                gravity,
                density,
                PhysicsConstants.DefaultDragCoefficient,
                PhysicsConstants.DefaultCrossSectionalArea);

        return (gravity, terminalVelocity, walkSpeed, canJump);
    }

    private void HandleJumpAndVerticalMovement(IPhysicsEntity entity, MovementIntent intent, float deltaTime, float walkSpeed, bool canJump)
    {
        var velocity = entity.Velocity;
        if (intent.IsFlying)
        {
            if (intent.IsJumping)
                velocity.Y = MathUtils.Lerp(velocity.Y, walkSpeed, deltaTime * 10f);
            else if (intent.IsDescending)
                velocity.Y = MathUtils.Lerp(velocity.Y, -walkSpeed, deltaTime * 10f);
            else
                velocity.Y = MathUtils.Lerp(velocity.Y, 0, deltaTime * 10f);
        }
        else if (intent.IsJumping)
        {
            if (SensorData?.IsSwimming ?? false)
            {
                // Provide a baseline upward movement when swimming
                velocity.Y = Math.Max(velocity.Y, 2.0f);

                // If deep enough, allow a stronger "kick" (jump)
                if ((SensorData?.SubmersionDepth ?? 0f) > 0.8f)
                {
                    velocity.Y = Math.Max(velocity.Y, 4.0f);
                }
            }
            else if (canJump)
            {
                velocity.Y = 5.0f; // Normal Jump
            }
        }
        entity.Velocity = velocity;
    }

    private void ApplyMovementInternal(IPhysicsEntity entity, MovementIntent intent, float gravity, float terminalVelocity, float walkSpeed, float deltaTime)
    {
        var velocity = entity.Velocity;

        // Dolphin-launch prevention: apply extra drag at the surface
        if ((SensorData?.IsSwimming ?? false) && velocity.Y > 3.5f && !(SensorData?.IsUnderwater ?? false))
        {
            velocity.Y = MathUtils.Lerp(velocity.Y, 3.5f, deltaTime * 10.0f);
        }

        // friction application
        var deltaFriction = 1.0f - MathF.Pow(1.0f - Friction, deltaTime * 60.0f);
        
        var moveDir = intent.Direction;
        if (!intent.IsFlying)
            moveDir.Y = 0;
            
        if (moveDir.LengthSquared() > 0)
            moveDir = Vector3.Normalize(moveDir);

        velocity.X = MathUtils.Lerp(velocity.X, moveDir.X * walkSpeed, deltaFriction);
        velocity.Z = MathUtils.Lerp(velocity.Z, moveDir.Z * walkSpeed, deltaFriction);

        // Apply gravity and clamp to terminal velocity
        velocity.Y += gravity * deltaTime;
        if (velocity.Y < terminalVelocity)
            velocity.Y = terminalVelocity;

        entity.Velocity = velocity;
    }
}