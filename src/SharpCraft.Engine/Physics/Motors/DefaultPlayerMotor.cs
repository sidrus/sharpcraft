using SharpCraft.Sdk.Physics;
using SharpCraft.Sdk.Physics.Motors;
using SharpCraft.Engine.Physics.Sensors.Spatial;

namespace SharpCraft.Engine.Physics.Motors;

/// <summary>
/// The default player motor. Rather than handling every medium in one tangle of branches,
/// it selects a dedicated motor for the current medium — <see cref="WalkingMotor"/>,
/// <see cref="SwimmingMotor"/> or <see cref="FlyingMotor"/> — and delegates to it each tick.
/// </summary>
public class DefaultPlayerMotor : IMotor
{
    private readonly WalkingMotor _walking = new();
    private readonly SwimmingMotor _swimming = new();
    private readonly FlyingMotor _flying = new();

    private PlayerMotorBase _active;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPlayerMotor"/> class.
    /// </summary>
    public DefaultPlayerMotor() => _active = _walking;

    /// <summary>
    /// Gets or sets the sensor data used to choose and drive the active motor.
    /// </summary>
    public GeospatialSensorData? SensorData { get; set; }

    /// <summary>
    /// Gets the friction coefficient applied by the active motor on its last pass.
    /// </summary>
    public float Friction => _active.Friction;

    /// <inheritdoc />
    public void ApplyForces(IPhysicsEntity entity, MovementIntent intent, float deltaTime)
    {
        _active = SelectMotor(intent);
        _active.SensorData = SensorData;
        _active.ApplyForces(entity, intent, deltaTime);
    }

    private PlayerMotorBase SelectMotor(MovementIntent intent)
    {
        if (intent.IsFlying)
            return _flying;

        var inWater = (SensorData?.IsSwimming ?? false) || (SensorData?.IsOnWaterSurface ?? false);
        return inWater ? _swimming : _walking;
    }
}
