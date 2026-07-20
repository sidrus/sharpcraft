using AwesomeAssertions;
using SharpCraft.Engine.Blocks;
using SharpCraft.Engine.Physics.Sensors.Spatial;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Resources;

namespace SharpCraft.Engine.Tests.Physics;

public class MaterialSensorTests
{
    private static readonly ResourceLocation DirtLoc = new("test", "dirt");
    private static readonly ResourceLocation WaterLoc = new("test", "water");
    private static readonly FluidProperties Water = new(1000f, 0.15f, -2f, 2f, 4f, 0.8f, -2f, 0.5f);

    private static BlockRegistry Registry()
    {
        var registry = new BlockRegistry();
        registry.Register(DirtLoc, new BlockDefinition(DirtLoc, "Dirt", Friction: 0.5f));
        registry.Register(WaterLoc, new BlockDefinition(WaterLoc, "Water", IsSolid: false, IsTransparent: true, Fluid: Water));
        return registry;
    }

    [Fact]
    public void Sense_BlockBelowRegistered_ShouldResolveGroundFriction()
    {
        var registry = Registry();
        var spatial = new GeospatialSensorData { BlockBelow = new Block(registry.GetId(DirtLoc), BlockFlags.Solid) };

        new MaterialSensor(registry).Sense(spatial).GroundFriction.Should().Be(0.5f);
    }

    [Fact]
    public void Sense_MidBlockIsFluid_ShouldResolveFluidProperties()
    {
        var registry = Registry();
        var spatial = new GeospatialSensorData
        {
            BlockAtMid = new Block(registry.GetId(WaterLoc), BlockFlags.Transparent | BlockFlags.Fluid)
        };

        new MaterialSensor(registry).Sense(spatial).Fluid.Should().Be(Water);
    }

    [Fact]
    public void Sense_NoFluidBlock_ShouldReturnNullFluid()
    {
        var registry = Registry();
        var spatial = new GeospatialSensorData { BlockBelow = new Block(registry.GetId(DirtLoc), BlockFlags.Solid) };

        new MaterialSensor(registry).Sense(spatial).Fluid.Should().BeNull();
    }
}
