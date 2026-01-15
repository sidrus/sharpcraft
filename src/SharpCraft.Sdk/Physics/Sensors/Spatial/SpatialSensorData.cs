using SharpCraft.Sdk.Blocks;

namespace SharpCraft.Sdk.Physics.Sensors.Spatial;

/// <summary>
/// Data captured by a spatial sensor regarding the surrounding environment.
/// </summary>
public record SpatialSensorData
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
    /// Gets the friction of the block below.
    /// </summary>
    public float BelowFriction { get; init; }

    /// <summary>
    /// Gets whether the block below is solid.
    /// </summary>
    public bool BelowIsSolid { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entity is currently swimming.
    /// </summary>
    public bool IsSwimming { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entity is fully underwater.
    /// </summary>
    public bool IsUnderwater { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entity is on the water surface.
    /// </summary>
    public bool IsOnWaterSurface { get; init; }

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
}