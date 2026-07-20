using System.Numerics;
using SharpCraft.Engine.Physics;
using SharpCraft.Engine.Physics.Motors;
using SharpCraft.Sdk.Physics;
using AwesomeAssertions;
using NSubstitute;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Engine.Physics.Sensors.Spatial;

namespace SharpCraft.Engine.Tests.Physics;

public class DefaultPlayerMotorTests
{
    [Fact]
    public void ApplyForces_ShouldApplyGravity()
    {
        // Setup
        var mockPhysics = Substitute.For<IPhysicsSystem>();
        mockPhysics.MoveAndResolve(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector3>())
                   .Returns(ci => ci.ArgAt<Vector3>(0) + ci.ArgAt<Vector3>(1));

        var transform = new Transform { Position = new Vector3(0, 10, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics);
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
        var mockPhysics = Substitute.For<IPhysicsSystem>();
        mockPhysics.MoveAndResolve(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector3>())
                   .Returns(ci => ci.ArgAt<Vector3>(0) + ci.ArgAt<Vector3>(1));

        var transform = new Transform { Position = new Vector3(0, 10, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics);
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
        var mockPhysics = Substitute.For<IPhysicsSystem>();
        mockPhysics.MoveAndResolve(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector3>())
                   .Returns(ci => ci.ArgAt<Vector3>(0) + ci.ArgAt<Vector3>(1));

        var transform = new Transform { Position = new Vector3(0, 1, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics);
        
        // Mock grounded state via sensor
        var motor = new DefaultPlayerMotor
        {
            SensorData = new GeospatialSensorData
            {
                IsGrounded = true,
                BlockBelow = new Block { Type = BlockType.Stone } // Solid block
            }
        };

        // Act
        motor.ApplyForces(entity, new MovementIntent { IsJumping = true }, 0.016f);
        entity.Update(0.016f);

        // Assert
        entity.Velocity.Y.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ApplyForces_WhenJumpingOnWaterSurface_ShouldApplyUpwardVelocity()
    {
        // Setup
        var mockPhysics = Substitute.For<IPhysicsSystem>();
        mockPhysics.MoveAndResolve(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector3>())
                   .Returns(ci => ci.ArgAt<Vector3>(0) + ci.ArgAt<Vector3>(1));

        var transform = new Transform { Position = new Vector3(0, 63.5f, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics);

        // Bobbing at the surface next to a climbable ledge.
        var motor = new DefaultPlayerMotor
        {
            SensorData = new GeospatialSensorData
            {
                IsOnWaterSurface = true,
                IsNextToClimbableLedge = true,
                BlockBelow = new Block { Type = BlockType.Water }
            }
        };

        // Act
        motor.ApplyForces(entity, new MovementIntent { IsJumping = true }, 0.016f);

        // Assert: the player should get a real upward impulse to climb out onto land,
        // not be pinned at the surface by full gravity.
        entity.Velocity.Y.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ApplyForces_WhenJumpingOnOpenWaterSurface_ShouldNotApplyUpwardVelocity()
    {
        // Setup
        var mockPhysics = Substitute.For<IPhysicsSystem>();
        mockPhysics.MoveAndResolve(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector3>())
                   .Returns(ci => ci.ArgAt<Vector3>(0) + ci.ArgAt<Vector3>(1));

        var transform = new Transform { Position = new Vector3(0, 63.5f, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics);

        // Bobbing at the surface in open water, no ledge to climb onto.
        var motor = new DefaultPlayerMotor
        {
            SensorData = new GeospatialSensorData
            {
                IsOnWaterSurface = true,
                IsNextToClimbableLedge = false,
                BlockBelow = new Block { Type = BlockType.Water }
            }
        };

        // Act: spamming jump in open water must not produce upward velocity
        // (otherwise the player could "walk on water").
        motor.ApplyForces(entity, new MovementIntent { IsJumping = true }, 0.016f);

        // Assert
        entity.Velocity.Y.Should().BeLessThanOrEqualTo(0);
    }

    [Fact]
    public void ApplyForces_ShouldApplyHorizontalMovement()
    {
        // Setup
        var mockPhysics = Substitute.For<IPhysicsSystem>();
        mockPhysics.MoveAndResolve(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector3>())
                   .Returns(ci => ci.ArgAt<Vector3>(0) + ci.ArgAt<Vector3>(1));

        var transform = new Transform { Position = new Vector3(0, 10, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics);
        var motor = new DefaultPlayerMotor();

        // Act
        motor.ApplyForces(entity, new MovementIntent { Direction = new Vector3(1, 0, 0) }, 1.0f);
        entity.Update(1.0f);

        // Assert
        entity.Velocity.X.Should().BeGreaterThan(0);
        entity.Position.X.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ApplyForces_WhenSprinting_ShouldMoveFaster()
    {
        // Setup
        var mockPhysics = Substitute.For<IPhysicsSystem>();
        mockPhysics.MoveAndResolve(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector3>())
                   .Returns(ci => ci.ArgAt<Vector3>(0) + ci.ArgAt<Vector3>(1));

        var motor = new DefaultPlayerMotor();
        var deltaTime = 1.0f;

        // Walk
        var entityWalk = new PhysicsEntity(new Transform { Position = Vector3.Zero }, mockPhysics);
        motor.ApplyForces(entityWalk, new MovementIntent { Direction = Vector3.UnitX, IsSprinting = false }, deltaTime);
        entityWalk.Update(deltaTime);

        // Sprint
        var entitySprint = new PhysicsEntity(new Transform { Position = Vector3.Zero }, mockPhysics);
        motor.ApplyForces(entitySprint, new MovementIntent { Direction = Vector3.UnitX, IsSprinting = true }, deltaTime);
        entitySprint.Update(deltaTime);

        // Assert
        entitySprint.Velocity.X.Should().BeGreaterThan(entityWalk.Velocity.X);
    }
}
