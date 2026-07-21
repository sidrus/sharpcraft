using AwesomeAssertions;
using NSubstitute;
using SharpCraft.Engine.Physics;
using SharpCraft.Engine.Physics.Motors;
using SharpCraft.Sdk.Physics;
using System.Numerics;

namespace SharpCraft.Engine.Tests.Physics;

public class WalkingMotorTests
{
    private static PhysicsEntity FallingEntity()
    {
        var physics = Substitute.For<IPhysicsSystem>();
        return new PhysicsEntity(new Transform { Position = new Vector3(0, 80f, 0) }, physics);
    }

    [Fact]
    public void ApplyForces_WhenAirborneForOneTick_ShouldAccumulateGravity()
    {
        var entity = FallingEntity();
        var motor = new WalkingMotor();
        const float deltaTime = 0.1f;

        motor.ApplyForces(entity, new MovementIntent(), deltaTime);

        entity.Velocity.Y.Should().BeApproximately(PhysicsConstants.DefaultGravity * deltaTime, 0.0001f);
    }

    [Fact]
    public void ApplyForces_WhenFallingIndefinitely_ShouldClampAtTerminalVelocity()
    {
        var entity = FallingEntity();
        var motor = new WalkingMotor();
        const float deltaTime = 0.1f;

        for (var i = 0; i < 2000; i++)
        {
            motor.ApplyForces(entity, new MovementIntent(), deltaTime);
        }

        var settled = entity.Velocity.Y;
        motor.ApplyForces(entity, new MovementIntent(), deltaTime);

        entity.Velocity.Y.Should().Be(settled);
        settled.Should().BeLessThan(0f);
    }
}