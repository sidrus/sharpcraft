using System.Numerics;

namespace SharpCraft.Core.Numerics;

/// <summary>
/// Provides mathematical utility functions.
/// </summary>
public static class MathUtils
{
    private static readonly Vector2[] Gradients =
    [
        new(1,1), new(-1,1), new(1,-1), new(-1,-1),
        new(1,0), new(-1,0), new(1,0), new(-1,0),
        new(0,1), new(0,-1), new(0,1), new(0,-1)
    ];

    /// <summary>
    /// Calculates the largest integer less than or equal to the specified float.
    /// </summary>
    /// <param name="x">The value to floor.</param>
    /// <returns>The floored value.</returns>
    public static int FastFloor(float x) => (int)x - (x < (int)x ? 1 : 0);

    /// <summary>
    /// Computes the dot product of a gradient vector and a 2D offset.
    /// </summary>
    /// <param name="g">The index of the gradient vector.</param>
    /// <param name="x">The X component of the offset.</param>
    /// <param name="y">The Y component of the offset.</param>
    /// <returns>The resulting dot product.</returns>
    public static float Dot(int g, float x, float y)
    {
        var grad = Gradients[g];
        return grad.X * x + grad.Y * y;
    }

    /// <summary>
    /// Performs linear interpolation between two values.
    /// </summary>
    /// <param name="a">The start value.</param>
    /// <param name="b">The end value.</param>
    /// <param name="t">The interpolation factor [0, 1].</param>
    /// <returns>The interpolated value.</returns>
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    /// <summary>
    /// Converts a yaw angle in degrees to a cardinal or intercardinal direction.
    /// </summary>
    /// <param name="yaw">The yaw angle in degrees (0 = North, 90 = West).</param>
    /// <returns>A string representing the heading.</returns>
    public static string GetHeading(float yaw)
    {
        var heading = (-yaw % 360 + 360) % 360;
        return heading switch
        {
            >= 337.5f or < 22.5f => "North",
            >= 22.5f and < 67.5f => "North-East",
            >= 67.5f and < 112.5f => "East",
            >= 112.5f and < 157.5f => "South-East",
            >= 157.5f and < 202.5f => "South",
            >= 202.5f and < 247.5f => "South-West",
            >= 247.5f and < 292.5f => "West",
            >= 292.5f and < 337.5f => "North-West",
            _ => "North"
        };
    }
}