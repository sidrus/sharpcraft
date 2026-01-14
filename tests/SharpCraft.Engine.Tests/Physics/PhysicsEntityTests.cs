using System.Numerics;
using SharpCraft.Engine.Physics;
using SharpCraft.Sdk.Physics;
using AwesomeAssertions;
using Moq;

namespace SharpCraft.Engine.Tests.Physics;

public class PhysicsEntityTests
{
    [Fact]
    public void Update_ShouldCallMoveAndResolve()
    {
        var mockPhysics = new Mock<IPhysicsSystem>();
        var transform = new Transform { Position = new Vector3(0, 10, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics.Object);
        entity.Velocity = new Vector3(0, -1, 0);
        var deltaTime = 1.0f;

        mockPhysics.Setup(p => p.MoveAndResolve(It.IsAny<Vector3>(), It.IsAny<Vector3>(), It.IsAny<Vector3>()))
                   .Returns(new Vector3(0, 9, 0));

        entity.Update(deltaTime);

        mockPhysics.Verify(p => p.MoveAndResolve(new Vector3(0, 10, 0), new Vector3(0, -1, 0), entity.Size), Times.Once);
        entity.Position.Y.Should().Be(9);
    }

    [Fact]
    public void Update_ShouldUpdatePreviousPosition()
    {
        var mockPhysics = new Mock<IPhysicsSystem>();
        var transform = new Transform { Position = new Vector3(0, 10, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics.Object);
        
        mockPhysics.Setup(p => p.MoveAndResolve(It.IsAny<Vector3>(), It.IsAny<Vector3>(), It.IsAny<Vector3>()))
                   .Returns(new Vector3(0, 9, 0));

        entity.Update(1.0f);

        entity.PreviousPosition.Y.Should().Be(10);
    }

    [Fact]
    public void IsGrounded_ShouldBeTrue_WhenVerticalMovementIsBlockedByFloor()
    {
        var mockPhysics = new Mock<IPhysicsSystem>();
        var transform = new Transform { Position = new Vector3(0, 1.001f, 0) };
        var entity = new PhysicsEntity(transform, mockPhysics.Object);
        entity.Velocity = new Vector3(0, -1, 0); // Moving down
        
        // Mock physics: return 1.001 instead of 0.001 (floor hit)
        mockPhysics.Setup(p => p.MoveAndResolve(It.IsAny<Vector3>(), It.IsAny<Vector3>(), It.IsAny<Vector3>()))
                   .Returns(new Vector3(0, 1.001f, 0));

        entity.Update(1.0f);

        entity.IsGrounded.Should().BeTrue();
    }
}
