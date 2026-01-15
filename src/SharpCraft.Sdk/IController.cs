using SharpCraft.Sdk.Physics.Motors;
using SharpCraft.Sdk.Physics.Sensors;
using SharpCraft.Sdk.Physics.Sensors.Spatial;

namespace SharpCraft.Sdk;

/// <summary>
/// Represents a specialized actor that can control movement and sense its surroundings.
/// </summary>
public interface IController : IActor
{
    /// <summary>
    /// Gets the motor used for driving movement.
    /// </summary>
    public IMotor Motor { get; }

    /// <summary>
    /// Gets the spatial sensor for perceiving the environment.
    /// </summary>
    public ISensor<SpatialSensorData> SpatialSensor { get; }
}