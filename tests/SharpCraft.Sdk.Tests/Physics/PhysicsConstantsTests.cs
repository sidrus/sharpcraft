using SharpCraft.Sdk.Physics;
using AwesomeAssertions;

namespace SharpCraft.Sdk.Tests.Physics;

public class PhysicsConstantsTests
{
    [Fact]
    public void CalculateTerminalVelocity_ShouldReturnExpectedValue_InAir()
    {
        // For a typical human: m=80, g=9.81, rho=1.225, Cd=1.0, A=0.5
        var vt = PhysicsConstants.CalculateTerminalVelocity(
            PhysicsConstants.DefaultMass,
            PhysicsConstants.DefaultGravity,
            PhysicsConstants.AirDensity,
            PhysicsConstants.DefaultDragCoefficient,
            PhysicsConstants.DefaultCrossSectionalArea);

        // vt = sqrt((2 * 80 * 9.81) / (1.225 * 1.0 * 0.5))
        // vt = sqrt(1569.6 / 0.6125)
        // vt = sqrt(2562.6122...) approx 50.62
        vt.Should().BeApproximately(50.62f, 0.01f);
    }

    [Fact]
    public void CalculateTerminalVelocity_ShouldReturnExpectedValue_InWater()
    {
        // In water with reduced gravity (buoyancy)
        var vt = PhysicsConstants.CalculateTerminalVelocity(
            PhysicsConstants.DefaultMass,
            PhysicsConstants.WaterGravity,
            PhysicsConstants.WaterDensity,
            PhysicsConstants.DefaultDragCoefficient,
            PhysicsConstants.DefaultCrossSectionalArea);

        // vt = sqrt((2 * 80 * 2.0) / (1000 * 1.0 * 0.5))
        // vt = sqrt(320 / 500)
        // vt = sqrt(0.64) = 0.8
        vt.Should().Be(0.8f);
    }

    [Fact]
    public void CalculateTerminalVelocity_ShouldReturnDefaultValue_WhenGravityIsZero()
    {
        var vt = PhysicsConstants.CalculateTerminalVelocity(80, 0, 1.225f, 1, 0.5f);
        vt.Should().Be(100f);
    }

    [Fact]
    public void CalculateTerminalVelocity_ShouldReturnDefaultValue_WhenDensityIsZero()
    {
        var vt = PhysicsConstants.CalculateTerminalVelocity(80, -9.81f, 0, 1, 0.5f);
        vt.Should().Be(100f);
    }
}
