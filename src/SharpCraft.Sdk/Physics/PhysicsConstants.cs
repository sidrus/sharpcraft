using System.Numerics;

namespace SharpCraft.Sdk.Physics;

/// <summary>
/// Provides common physics constants and utility functions.
/// </summary>
public static class PhysicsConstants
{
    /// <summary>
    /// The default gravity acceleration on Earth (m/s^2).
    /// </summary>
    public const float DefaultGravity = -9.81f;

    /// <summary>
    /// The gravity acceleration in water, accounting for buoyancy (m/s^2).
    /// </summary>
    public const float WaterGravity = -2.0f;

    /// <summary>
    /// The density of air at sea level (kg/m^3).
    /// </summary>
    public const float AirDensity = 1.225f;

    /// <summary>
    /// The density of pure water (kg/m^3).
    /// </summary>
    public const float WaterDensity = 1000f;

    /// <summary>
    /// Default drag coefficient for a human-like entity.
    /// </summary>
    public const float DefaultDragCoefficient = 1.0f;

    /// <summary>
    /// Default cross-sectional area for a human-like entity (m^2).
    /// </summary>
    public const float DefaultCrossSectionalArea = 0.5f;

    /// <summary>
    /// Default mass for a human-like entity (kg).
    /// </summary>
    public const float DefaultMass = 80f;

    /// <summary>
    /// A small value used for floating-point comparisons and collision offsets.
    /// </summary>
    public const float Epsilon = 0.001f;

    /// <summary>
    /// Default friction coefficient in air.
    /// </summary>
    public const float AirFriction = 0.05f;

    /// <summary>
    /// Default friction coefficient in water.
    /// </summary>
    public const float WaterFriction = 0.15f;

    /// <summary>
    /// Default friction coefficient when flying.
    /// </summary>
    public const float FlyingFriction = 0.15f;

    /// <summary>
    /// Calculates the terminal velocity of an object based on the drag equation.
    /// </summary>
    /// <param name="mass">The mass of the object (kg).</param>
    /// <param name="gravity">The gravity acceleration (m/s^2).</param>
    /// <param name="density">The density of the fluid (kg/m^3).</param>
    /// <param name="dragCoefficient">The drag coefficient (dimensionless).</param>
    /// <param name="area">The cross-sectional area (m^2).</param>
    /// <returns>The terminal velocity (m/s).</returns>
    public static float CalculateTerminalVelocity(
        float mass,
        float gravity,
        float density,
        float dragCoefficient,
        float area)
    {
        var absGravity = MathF.Abs(gravity);
        
        // Prevent division by zero or negative results
        if (absGravity < 1e-6f) return 100f;
        
        var denominator = density * dragCoefficient * area;
        if (denominator < 1e-6f) return 100f;

        return MathF.Sqrt((2.0f * mass * absGravity) / denominator);
    }
}
