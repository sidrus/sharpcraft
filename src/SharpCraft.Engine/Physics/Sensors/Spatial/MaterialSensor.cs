using SharpCraft.Sdk.Blocks;

namespace SharpCraft.Engine.Physics.Sensors.Spatial;

/// <summary>
/// Resolves block-material properties (ground friction, current fluid) from the block registry,
/// given the spatial blocks a <see cref="GeospatialSensor"/> already probed.
/// </summary>
public class MaterialSensor(IBlockRegistry blocks)
{
    /// <summary>
    /// Resolves material properties for the entity described by the spatial sensor data.
    /// </summary>
    public MaterialSensorData Sense(GeospatialSensorData spatial)
    {
        var fluidBlock = spatial.BlockAtMid.IsFluid ? spatial.BlockAtMid
            : spatial.BlockAbove.IsFluid ? spatial.BlockAbove
            : spatial.BlockBelow.IsFluid ? spatial.BlockBelow
            : Block.Air;

        return new MaterialSensorData
        {
            GroundFriction = blocks.GetById(spatial.BlockBelow.Id)?.Friction ?? 0f,
            Fluid = fluidBlock.IsFluid ? blocks.GetById(fluidBlock.Id)?.Fluid : null,
        };
    }
}
