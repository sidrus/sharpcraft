namespace SharpCraft.Sdk.Physics.Sensors;

/// <summary>
/// Defines a sensor that can perceive data from the physics world.
/// </summary>
/// <typeparam name="TSensorData">The type of data produced by the sensor.</typeparam>
public interface ISensor<out TSensorData>
{
    /// <summary>
    /// Gets the most recent data captured by the sensor.
    /// </summary>
    public TSensorData? LastSense { get; }

    /// <summary>
    /// Performs a sensing operation in the specified world for the given entity.
    /// </summary>
    /// <param name="world">The collision provider to sense from.</param>
    /// <param name="entity">The entity performing the sensing.</param>
    /// <returns>The captured sensor data.</returns>
    public TSensorData Sense(ICollisionProvider world, IPhysicsEntity entity);
}