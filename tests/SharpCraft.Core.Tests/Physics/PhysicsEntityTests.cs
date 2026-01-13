using System.Numerics;
using SharpCraft.Core.Physics;
using AwesomeAssertions;

namespace SharpCraft.Core.Tests.Physics;

public class PhysicsEntityTests
{
    private class FakePhysicsSystem : IPhysicsSystem
    {
        public Vector3 MoveAndResolve(Vector3 position, Vector3 velocity, Vector3 size)
        {
            // Simple pass-through for most tests, or custom logic if needed
            return position + velocity;
        }
    }

    [Fact]
    public void Update_ShouldUpdatePosition_BasedOnVelocity()
    {
        var transform = new Transform { Position = new Vector3(0, 10, 0) };
        var physics = new FakePhysicsSystem();
        var entity = new PhysicsEntity(transform, physics);
        entity.Velocity = new Vector3(10, 0, 0);

        entity.Update(0.1f);

        entity.Position.X.Should().Be(1f);
        entity.PreviousPosition.X.Should().Be(0f);
    }

    [Fact]
    public void Update_ShouldResetVelocity_WhenColliding()
    {
        var transform = new Transform { Position = new Vector3(0, 10, 0) };
        var physics = new CollidingPhysicsSystem();
        var entity = new PhysicsEntity(transform, physics);
        entity.Velocity = new Vector3(10, 10, 10);

        entity.Update(0.1f);

        // CollidingPhysicsSystem returns fixed position (0.5, 10.5, 0.5)
        // expected without collision: (1.0, 11.0, 1.0)
        // diff is > 0.001, so velocity should be reset to 0
        entity.Velocity.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Update_ShouldSetIsGrounded_WhenHittingFloor()
    {
        var transform = new Transform { Position = new Vector3(0, 10, 0) };
        var physics = new GroundingPhysicsSystem();
        var entity = new PhysicsEntity(transform, physics);
        entity.Velocity = new Vector3(0, -10, 0); // Moving down

        entity.Update(0.1f);

        // GroundingPhysicsSystem returns Y = 10.001
        // oldY = 10, velocity * dt = -1. 10 - 1 = 9.
        // 10.001 > 9 + 0.0001, and preV.Y <= 0.
        entity.IsGrounded.Should().BeTrue();
    }

    [Fact]
    public void Forward_ShouldReturnNormalizedVector()
    {
        var transform = new Transform();
        var physics = new FakePhysicsSystem();
        var entity = new PhysicsEntity(transform, physics);
        
        // Identity rotation, Forward should be -UnitZ
        entity.Forward.Should().Be(-Vector3.UnitZ);
    }

    [Fact]
    public void Right_ShouldReturnNormalizedVector()
    {
        var transform = new Transform();
        var physics = new FakePhysicsSystem();
        var entity = new PhysicsEntity(transform, physics);

        // Identity rotation, Right should be UnitX
        entity.Right.Should().Be(Vector3.UnitX);
    }

    [Fact]
    public void Rotation_ShouldAffectForwardAndRight()
    {
        var transform = new Transform();
        var physics = new FakePhysicsSystem();
        var entity = new PhysicsEntity(transform, physics);

        // Rotate 90 degrees around Y axis
        entity.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);

        // Forward was -Z. Rotating -Z by 90 deg around Y gives -X in this system.
        entity.Forward.X.Should().BeApproximately(-1f, 0.0001f);
        entity.Forward.Z.Should().BeApproximately(0f, 0.0001f);

        // Right was X. Rotating X by 90 deg around Y gives -Z in this system.
        entity.Right.X.Should().BeApproximately(0f, 0.0001f);
        entity.Right.Z.Should().BeApproximately(-1f, 0.0001f);
    }

    private class CollidingPhysicsSystem : IPhysicsSystem
    {
        public Vector3 MoveAndResolve(Vector3 position, Vector3 velocity, Vector3 size)
        {
            return new Vector3(0.5f, 10.5f, 0.5f); // Blocked at 0.5
        }
    }

    private class GroundingPhysicsSystem : IPhysicsSystem
    {
        public Vector3 MoveAndResolve(Vector3 position, Vector3 velocity, Vector3 size)
        {
            return new Vector3(position.X, 10.001f, position.Z);
        }
    }
}
