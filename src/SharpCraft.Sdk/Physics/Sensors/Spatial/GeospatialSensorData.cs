namespace SharpCraft.Sdk.Physics.Sensors.Spatial;

/// <summary>
/// Data captured by a geospatial sensor, including orientation.
/// </summary>
public record GeospatialSensorData : SpatialSensorData
{
    /// <summary>
    /// Gets the horizontal orientation (heading) in degrees.
    /// </summary>
    public float Heading { get; init; }

    /// <summary>
    /// Gets the vertical orientation (pitch) in degrees.
    /// </summary>
    public float Pitch { get; init; }

    /// <summary>
    /// Gets the bank orientation (roll) in degrees.
    /// </summary>
    public float Roll { get; init; }
}