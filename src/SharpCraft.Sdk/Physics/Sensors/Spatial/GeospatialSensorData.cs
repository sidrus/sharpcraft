namespace SharpCraft.Sdk.Physics.Sensors.Spatial;

public record GeospatialSensorData : SpatialSensorData
{
    public float Heading { get; init; }
    public float Pitch { get; init; }
    public float Roll { get; init; }
}