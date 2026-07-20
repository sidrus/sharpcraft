using System.Numerics;
using SharpCraft.Engine.Physics;
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

    [Fact]
    public void Sense_ShouldPopulateBasicData()
    {
        // Arrange
        var sensor = new GeospatialSensor();
        var entity = new PhysicsEntity(new Transform { Position = new Vector3(0.5f, 1.0f, 0.5f) }, _physicsMock.Object);

        _collisionProviderMock.Setup(w => w.GetBlock(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new Block { Type = BlockType.Air });

        // Act
        var result = sensor.Sense(_collisionProviderMock.Object, entity);

        // Assert
        result.Should().NotBeNull();
        result.IsFlying.Should().BeTrue();
    }

    [Fact]
    public void Sense_ShouldIncludeOrientation()
    {
        // Arrange
        var sensor = new GeospatialSensor();
        var transform = new Transform
        {
            Position = new Vector3(0.5f, 1.0f, 0.5f),
            Rotation = Quaternion.CreateFromYawPitchRoll(1.0f, 0.5f, 0.0f)
        };
        var entity = new PhysicsEntity(transform, _physicsMock.Object);

        _collisionProviderMock.Setup(w => w.GetBlock(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new Block { Type = BlockType.Air });

        // Act
        var result = sensor.Sense(_collisionProviderMock.Object, entity);

        // Assert
        result.Heading.Should().BeApproximately(1.0f * 180f / MathF.PI, 0.01f);
        result.Pitch.Should().BeApproximately(0.5f * 180f / MathF.PI, 0.01f);
    }
}
