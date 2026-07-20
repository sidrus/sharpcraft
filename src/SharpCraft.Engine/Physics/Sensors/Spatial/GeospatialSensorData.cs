using SharpCraft.Sdk.Blocks;

namespace SharpCraft.Engine.Physics.Sensors.Spatial;

/// <summary>
/// Data captured by a geospatial sensor: the surrounding environment plus the entity's
/// orientation (heading and pitch).
/// </summary>
public record GeospatialSensorData
{
    /// <summary>
    /// Gets the block directly above the entity.
    /// </summary>
    public Block BlockAbove { get; init; }

    /// <summary>
    /// Gets the block directly below the entity.
    /// </summary>
    public Block BlockBelow { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entity is currently swimming.
    /// </summary>
    public bool IsSwimming { get; init; }

    /// <summary>
    /// Gets the block at the entity's mid-body.
    /// </summary>
    public Block BlockAtMid { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entity is fully submerged in a fluid.
    /// </summary>
    public bool IsSubmerged { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entity is on a fluid surface.
    /// </summary>
    public bool IsOnFluidSurface { get; init; }

    /// <summary>
    /// Gets a value indicating whether a solid, step-height ledge is adjacent that the
    /// entity could climb onto from the water surface. Used to allow jumping out onto
    /// land while still preventing the player from "walking on water" in open water.
    /// </summary>
    public bool IsNextToClimbableLedge { get; init; }

    /// <summary>
    /// Gets the depth to which the entity is submerged in a fluid.
    /// </summary>
    public float SubmersionDepth { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entity is currently flying.
    /// </summary>
    public bool IsFlying { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entity is touching the ground.
    /// </summary>
    public bool IsGrounded { get; init; }

    /// <summary>
    /// Gets the horizontal orientation (heading) in degrees.
    /// </summary>
    public float Heading { get; init; }

    /// <summary>
    /// Gets the vertical orientation (pitch) in degrees.
    /// </summary>
    public float Pitch { get; init; }
}