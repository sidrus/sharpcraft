using SharpCraft.Sdk.Blocks;

namespace SharpCraft.Engine.Physics.Sensors.Spatial;

/// <summary>
/// Block-material properties resolved for an entity's surroundings: ground friction and the
/// fluid it is in, if any.
/// </summary>
public record MaterialSensorData
{
    /// <summary>Gets the friction of the block underfoot.</summary>
    public float GroundFriction
    {
        get; init;
    }

    /// <summary>Gets the properties of the fluid the entity is in, or null.</summary>
    public FluidProperties? Fluid
    {
        get; init;
    }
}