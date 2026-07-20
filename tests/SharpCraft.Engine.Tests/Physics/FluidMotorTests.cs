using AwesomeAssertions;
using NSubstitute;
using SharpCraft.Engine.Physics;
using SharpCraft.Engine.Physics.Motors;
using SharpCraft.Engine.Physics.Sensors.Spatial;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Physics;
using System.Numerics;

namespace SharpCraft.Engine.Tests.Physics;

public class FluidMotorTests
{
    [Fact]
    public void ApplyForces_WhenSubmerged_ShouldApplyTheFluidsBuoyantGravity()
    {
        var physics = Substitute.For<IPhysicsSystem>();
        physics.MoveAndResolve(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector3>())
            .Returns(ci => ci.ArgAt<Vector3>(0) + ci.ArgAt<Vector3>(1));
        var entity = new PhysicsEntity(new Transform { Position = new Vector3(0, 62f, 0) }, physics);
        var fluid = new FluidProperties(1000f, 0.15f, BuoyantGravity: -0.5f, 2f, 4f, 0.8f, -2f, 0.5f);
        var motor = new FluidMotor
        {
            SensorData = new GeospatialSensorData { IsSubmerged = true },
            Material = new MaterialSensorData { Fluid = fluid },
        };
        const float deltaTime = 0.1f;

        motor.ApplyForces(entity, new MovementIntent(), deltaTime);

        entity.Velocity.Y.Should().BeApproximately(fluid.BuoyantGravity * deltaTime, 0.0001f);
    }
}