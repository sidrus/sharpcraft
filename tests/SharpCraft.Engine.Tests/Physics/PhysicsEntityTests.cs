using AwesomeAssertions;
using NSubstitute;
using SharpCraft.Engine.Physics;
using SharpCraft.Sdk.Physics;
using System.Numerics;

namespace SharpCraft.Engine.Tests.Physics;

public class PhysicsEntityTests
{
    [Fact]
    public void Update_ShouldCallMoveAndResolve()
    {
        var mockPhysics = Substitute.For<IPhysicsSystem>();
        var transform = new Transform { Position = new Vector3(0, 10, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics)
        {
            Velocity = new Vector3(0, -1, 0)
        };
        var deltaTime = 1.0f;

        mockPhysics.MoveAndResolve(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector3>())
                   .Returns(new Vector3(0, 9, 0));

        entity.Update(deltaTime);

        mockPhysics.Received(1).MoveAndResolve(new Vector3(0, 10, 0), new Vector3(0, -1, 0), entity.Size);
        entity.Position.Y.Should().Be(9);
    }

    [Fact]
    public void Update_ShouldUpdatePreviousPosition()
    {
        var mockPhysics = Substitute.For<IPhysicsSystem>();
        var transform = new Transform { Position = new Vector3(0, 10, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics);

        mockPhysics.MoveAndResolve(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector3>())
                   .Returns(new Vector3(0, 9, 0));

        entity.Update(1.0f);

        entity.PreviousPosition.Y.Should().Be(10);
    }

    [Fact]
    public void IsGrounded_ShouldBeTrue_WhenVerticalMovementIsBlockedByFloor()
    {
        var mockPhysics = Substitute.For<IPhysicsSystem>();
        var transform = new Transform { Position = new Vector3(0, 1.001f, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics)
        {
            Velocity = new Vector3(0, -1, 0) // Moving down
        };

        // Mock physics: return 1.001 instead of 0.001 (floor hit)
        mockPhysics.MoveAndResolve(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector3>())
                   .Returns(new Vector3(0, 1.001f, 0));

        entity.Update(1.0f);

        entity.IsGrounded.Should().BeTrue();
    }
}