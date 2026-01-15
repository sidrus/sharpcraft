using System.Numerics;
using SharpCraft.Engine.Physics;
using SharpCraft.Engine.Blocks;
using SharpCraft.Sdk.Physics;
using AwesomeAssertions;
using Moq;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Physics.Sensors.Spatial;

namespace SharpCraft.Engine.Tests.Physics;

public class SpatialSensorTests
{
    private readonly Mock<IPhysicsSystem> _physicsMock = new();
    private readonly Mock<ICollisionProvider> _collisionProviderMock = new();
    private readonly IBlockRegistry _blockRegistry = new BlockRegistry();

    public SpatialSensorTests()
    {
        _blockRegistry.Register(BlockIds.Air, new BlockDefinition(BlockIds.Air, "Air", IsSolid: false, IsTransparent: true));
        _blockRegistry.Register(BlockIds.Water, new BlockDefinition(BlockIds.Water, "Water", IsSolid: false, IsTransparent: true));
        _collisionProviderMock.Setup(c => c.Blocks).Returns(_blockRegistry);
    }

    [Fact]
    public void SpatialSensor_Sense_ShouldPopulateBasicData()
    {
        // Arrange
        var sensor = new SpatialSensor();
        var entity = new PhysicsEntity(new Transform { Position = new Vector3(0.5f, 1.0f, 0.5f) }, _physicsMock.Object);
        
        _collisionProviderMock.Setup(w => w.GetBlock(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new Block { Id = BlockIds.Air });

        // Act
        // We use a custom sensor that overrides CreateData to use our mock, 
        // since SpatialSensor.Sense(World, Entity) is what's public but we want to test the logic with a mock.
        // Or we can just use the GeospatialSensor which we know overrides it.
        // Actually, let's make a TestSpatialSensor to test the protected methods.
        var testSensor = new TestSpatialSensor(_collisionProviderMock.Object);

        var result = testSensor.Sense(null!, entity); 

        // Assert
        result.Should().NotBeNull();
        result.IsFlying.Should().BeTrue();
    }

    [Fact]
    public void GeospatialSensor_Sense_ShouldIncludeOrientation()
    {
        // Arrange
        var sensor = new TestGeospatialSensor(_collisionProviderMock.Object);
        var transform = new Transform 
        { 
            Position = new Vector3(0.5f, 1.0f, 0.5f),
            Rotation = Quaternion.CreateFromYawPitchRoll(1.0f, 0.5f, 0.0f) 
        };
        var entity = new PhysicsEntity(transform, _physicsMock.Object);
        
        _collisionProviderMock.Setup(w => w.GetBlock(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new Block { Id = BlockIds.Air });

        // Act
        var result = sensor.Sense(null!, entity);

        // Assert
        result.Should().BeOfType<GeospatialSensorData>();
        result.Heading.Should().BeApproximately(1.0f * 180f / MathF.PI, 0.01f);
        result.Pitch.Should().BeApproximately(0.5f * 180f / MathF.PI, 0.01f);
    }

    private class TestSpatialSensor(ICollisionProvider mockProvider) : SpatialSensor
    {
        protected override SpatialSensorData CreateData(ICollisionProvider collisionProvider, IPhysicsEntity entity)
        {
            return base.CreateData(mockProvider, entity);
        }
    }

    private class TestGeospatialSensor(ICollisionProvider mockProvider) : GeospatialSensor
    {
        protected override SpatialSensorData CreateData(ICollisionProvider collisionProvider, IPhysicsEntity entity)
        {
            return base.CreateData(mockProvider, entity);
        }
    }
}
