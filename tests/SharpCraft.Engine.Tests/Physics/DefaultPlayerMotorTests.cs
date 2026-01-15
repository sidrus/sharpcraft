using System.Numerics;
using SharpCraft.Engine.Physics;
using SharpCraft.Engine.Physics.Motors;
using SharpCraft.Sdk.Physics;
using AwesomeAssertions;
using Moq;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Physics.Sensors.Spatial;

namespace SharpCraft.Engine.Tests.Physics;

public class DefaultPlayerMotorTests
{
    [Fact]
    public void ApplyForces_ShouldApplyGravity()
    {
        // Setup
        var mockPhysics = new Mock<IPhysicsSystem>();
        mockPhysics.Setup(p => p.MoveAndResolve(It.IsAny<Vector3>(), It.IsAny<Vector3>(), It.IsAny<Vector3>()))
                   .Returns((Vector3 pos, Vector3 move, Vector3 size) => pos + move);

        var transform = new Transform { Position = new Vector3(0, 10, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics.Object);
        var motor = new DefaultPlayerMotor();
        var deltaTime = 1.0f;

        // Act
        motor.ApplyForces(entity, new MovementIntent(), deltaTime);
        entity.Update(deltaTime);

        // Assert
        // Initial velocity is 0, gravity is -9.81, so new velocity is -9.81
        // Position change should be roughly gravity * deltaTime
        entity.Velocity.Y.Should().BeApproximately(PhysicsConstants.DefaultGravity, 0.001f);
        entity.Position.Y.Should().BeApproximately(10f + PhysicsConstants.DefaultGravity, 0.001f);
    }

    [Fact]
    public void ApplyForces_WhenFlying_ShouldNotApplyGravity()
    {
        // Setup
        var mockPhysics = new Mock<IPhysicsSystem>();
        mockPhysics.Setup(p => p.MoveAndResolve(It.IsAny<Vector3>(), It.IsAny<Vector3>(), It.IsAny<Vector3>()))
                   .Returns((Vector3 pos, Vector3 move, Vector3 size) => pos + move);

        var transform = new Transform { Position = new Vector3(0, 10, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics.Object);
        var motor = new DefaultPlayerMotor();
        var deltaTime = 1.0f;

        // Act
        motor.ApplyForces(entity, new MovementIntent { IsFlying = true }, deltaTime);
        entity.Update(deltaTime);

        // Assert
        entity.Velocity.Y.Should().Be(0);
        entity.Position.Y.Should().Be(10);
    }

    [Fact]
    public void ApplyForces_WhenJumpingOnGround_ShouldApplyUpwardVelocity()
    {
        // Setup
        var mockPhysics = new Mock<IPhysicsSystem>();
        mockPhysics.Setup(p => p.MoveAndResolve(It.IsAny<Vector3>(), It.IsAny<Vector3>(), It.IsAny<Vector3>()))
                   .Returns((Vector3 pos, Vector3 move, Vector3 size) => pos + move);

        var transform = new Transform { Position = new Vector3(0, 1, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics.Object);
        
        // Mock grounded state via sensor
        var motor = new DefaultPlayerMotor
        {
            SensorData = new SpatialSensorData
            {
                IsGrounded = true,
                BlockBelow = new Block { Id = BlockIds.Stone },
                BelowIsSolid = true,
                BelowFriction = 0.5f
            }
        };

        // Act
        motor.ApplyForces(entity, new MovementIntent { IsJumping = true }, 0.016f);
        entity.Update(0.016f);

        // Assert
        entity.Velocity.Y.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ApplyForces_ShouldApplyHorizontalMovement()
    {
        // Setup
        var mockPhysics = new Mock<IPhysicsSystem>();
        mockPhysics.Setup(p => p.MoveAndResolve(It.IsAny<Vector3>(), It.IsAny<Vector3>(), It.IsAny<Vector3>()))
                   .Returns((Vector3 pos, Vector3 move, Vector3 size) => pos + move);

        var transform = new Transform { Position = new Vector3(0, 10, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics.Object);
        var motor = new DefaultPlayerMotor();

        // Act
        motor.ApplyForces(entity, new MovementIntent { Direction = new Vector3(1, 0, 0) }, 1.0f);
        entity.Update(1.0f);

        // Assert
        entity.Velocity.X.Should().BeGreaterThan(0);
        entity.Position.X.Should().BeGreaterThan(0);
    }
}
