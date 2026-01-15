using SharpCraft.Sdk.Physics.Motors;
using SharpCraft.Sdk.Physics.Sensors;
using SharpCraft.Sdk.Physics.Sensors.Spatial;

namespace SharpCraft.Sdk;

public interface IController : IActor
{
    public IMotor Motor { get; }

    public ISensor<SpatialSensorData> SpatialSensor { get; }
}