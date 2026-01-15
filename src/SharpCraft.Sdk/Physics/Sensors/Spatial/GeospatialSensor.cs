using SharpCraft.Sdk.Numerics;

namespace SharpCraft.Sdk.Physics.Sensors.Spatial;

/// <summary>
/// A specialized spatial sensor that also tracks geospatial orientation (heading, pitch, roll).
/// </summary>
public class GeospatialSensor : SpatialSensor, ISensor<GeospatialSensorData>
{
    /// <inheritdoc cref="ISensor{TSensorData}.LastSense" />
    public new GeospatialSensorData? LastSense { get; private set; }

    /// <inheritdoc cref="ISensor{TSensorData}.Sense" />
    public new GeospatialSensorData Sense(ICollisionProvider world, IPhysicsEntity entity)
    {
        return LastSense = (GeospatialSensorData)base.Sense(world, entity);
    }

    /// <inheritdoc />
    protected override SpatialSensorData CreateBaseData(ICollisionProvider world, IPhysicsEntity entity) => new GeospatialSensorData();

    /// <inheritdoc />
    protected override SpatialSensorData CreateData(ICollisionProvider world, IPhysicsEntity entity)
    {
        var data = (GeospatialSensorData)base.CreateData(world, entity);
        var (yaw, pitch, roll) = MathUtils.ToEulerAngles(entity.Rotation);

        return data with
        {
            Heading = yaw,
            Pitch = pitch,
            Roll = roll
        };
    }
}