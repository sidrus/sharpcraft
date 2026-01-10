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
}