using SharpCraft.Engine.Physics.Sensors.Spatial;
using SharpCraft.Sdk.Physics;
using SharpCraft.Sdk.Physics.Motors;
using System.Numerics;

namespace SharpCraft.Engine.Physics.Motors;

/// <summary>
/// Base class for the per-medium player motors (walking, swimming, flying). Each motor
/// owns the vertical behaviour for its medium; the shared horizontal movement, speed and
/// terminal-velocity helpers live here.
/// </summary>
public abstract class PlayerMotorBase : IMotor
{
    /// <summary>The base walking speed in m/s.</summary>
    protected const float WalkSpeed = 1.42f;

    /// <summary>The base sprinting speed in m/s.</summary>
    protected const float SprintSpeed = 3.84f;

    /// <summary>
    /// Gets or sets the sensor data describing the entity's surroundings.
    /// </summary>
    public GeospatialSensorData? SensorData { get; set; }

    /// <summary>
    /// Gets or sets the material properties (ground friction, current fluid) of those surroundings.
    /// </summary>
    public MaterialSensorData? Material { get; set; }

    /// <summary>
    /// Gets the friction coefficient the motor applied on its last pass.
    /// </summary>
    public float Friction { get; protected set; } = PhysicsConstants.AirFriction;

    /// <inheritdoc />
    public abstract void ApplyForces(IPhysicsEntity entity, MovementIntent intent, float deltaTime);

    /// <summary>
    /// Calculates the horizontal movement speed for the given intent, including sprint and
    /// the developer speed boost.
    /// </summary>
    protected static float GetBaseSpeed(MovementIntent intent)
    {
        var speed = intent.IsSprinting ? SprintSpeed : WalkSpeed;
        if (intent.UseDevSpeedBoost)
            speed *= 5;
        return speed;
    }

    /// <summary>
    /// Applies friction-based horizontal (X/Z) movement toward the intended direction.
    /// Vertical velocity is left untouched so each motor can own its own vertical model.
    /// </summary>
    protected void ApplyHorizontalMovement(IPhysicsEntity entity, MovementIntent intent, float speed, float deltaTime)
    {
        var velocity = entity.Velocity;
        var deltaFriction = 1.0f - MathF.Pow(1.0f - Friction, deltaTime * 60.0f);

        var moveDir = intent.Direction;
        moveDir.Y = 0;
        if (moveDir.LengthSquared() > 0)
            moveDir = Vector3.Normalize(moveDir);

        velocity.X = float.Lerp(velocity.X, moveDir.X * speed, deltaFriction);
        velocity.Z = float.Lerp(velocity.Z, moveDir.Z * speed, deltaFriction);

        entity.Velocity = velocity;
    }

    /// <summary>
    /// Computes the (negative) terminal velocity for falling through a fluid of the given
    /// density under the given gravity.
    /// </summary>
    protected static float ComputeTerminalVelocity(float gravity, float density)
        => -PhysicsConstants.CalculateTerminalVelocity(
            PhysicsConstants.DefaultMass,
            gravity,
            density,
            PhysicsConstants.DefaultDragCoefficient,
            PhysicsConstants.DefaultCrossSectionalArea);
}