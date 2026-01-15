using SharpCraft.Sdk.Numerics;

namespace SharpCraft.Sdk.Physics.Sensors.Spatial;

public class GeospatialSensor : SpatialSensor, ISensor<GeospatialSensorData>
{
    public new GeospatialSensorData? LastSense { get; private set; }

    public new GeospatialSensorData Sense(ICollisionProvider world, IPhysicsEntity entity)
    {
        return LastSense = (GeospatialSensorData)base.Sense(world, entity);
    }

    protected override SpatialSensorData CreateBaseData(ICollisionProvider world, IPhysicsEntity entity) => new GeospatialSensorData();

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