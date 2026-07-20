using AwesomeAssertions;
using NSubstitute;
using SharpCraft.Engine.Physics;
using SharpCraft.Engine.Physics.Sensors.Spatial;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Physics;
using System.Numerics;

namespace SharpCraft.Engine.Tests.Physics;

public class SpatialSensorTests
{
    private readonly IPhysicsSystem _physicsMock = Substitute.For<IPhysicsSystem>();
    private readonly ICollisionProvider _collisionProviderMock = Substitute.For<ICollisionProvider>();

    [Fact]
    public void Sense_ShouldPopulateBasicData()
    {
        // Arrange
        var sensor = new GeospatialSensor();
        var entity = new PhysicsEntity(new Transform { Position = new Vector3(0.5f, 1.0f, 0.5f) }, _physicsMock);

        _collisionProviderMock.GetBlock(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Block.Air);

        // Act
        var result = sensor.Sense(_collisionProviderMock, entity);

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
        var entity = new PhysicsEntity(transform, _physicsMock);

        _collisionProviderMock.GetBlock(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Block.Air);

        // Act
        var result = sensor.Sense(_collisionProviderMock, entity);

        // Assert
        result.Heading.Should().BeApproximately(1.0f * 180f / MathF.PI, 0.01f);
        result.Pitch.Should().BeApproximately(0.5f * 180f / MathF.PI, 0.01f);
    }
}