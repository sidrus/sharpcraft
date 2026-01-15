using SharpCraft.Sdk.Blocks;

namespace SharpCraft.Sdk.Physics.Sensors.Spatial;

public record SpatialSensorData
{
    public Block BlockAbove { get; init; }
    public Block BlockBelow { get; init; }
    public bool IsSwimming { get; init; }
    public bool IsUnderwater { get; init; }
    public bool IsOnWaterSurface { get; init; }
    public float SubmersionDepth { get; init; }
    public bool IsFlying { get; init; }
    public bool IsGrounded { get; init; }
}