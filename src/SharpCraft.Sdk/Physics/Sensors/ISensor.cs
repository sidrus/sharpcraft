namespace SharpCraft.Sdk.Physics.Sensors;

public interface ISensor<out TSensorData>
{
    public TSensorData? LastSense { get; }

    public TSensorData Sense(ICollisionProvider world, IPhysicsEntity entity);
}