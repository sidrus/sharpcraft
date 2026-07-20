using System.Numerics;

namespace SharpCraft.Sdk.Numerics;

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
    /// Extracts Euler angles (Yaw, Pitch, Roll) from a quaternion in degrees.
    /// </summary>
    /// <param name="q">The quaternion to convert.</param>
    /// <returns>A tuple containing (Yaw, Pitch, Roll) in degrees.</returns>
    public static (float Yaw, float Pitch, float Roll) ToEulerAngles(Quaternion q)
    {
        // yaw (y-axis rotation)
        var sinyCosp = 2 * (q.W * q.Y + q.X * q.Z);
        var cosyCosp = 1 - 2 * (q.Y * q.Y + q.X * q.X);
        var yaw = MathF.Atan2(sinyCosp, cosyCosp);

        // pitch (x-axis rotation)
        var sinp = 2 * (q.W * q.X - q.Z * q.Y);
        float pitch;
        if (MathF.Abs(sinp) >= 1)
        {
            pitch = MathF.CopySign(MathF.PI / 2, sinp); // use 90 degrees if out of range
        }
        else
        {
            pitch = MathF.Asin(sinp);
        }

        // roll (z-axis rotation)
        var sinrCosp = 2 * (q.W * q.Z + q.X * q.Y);
        var cosrCosp = 1 - 2 * (q.X * q.X + q.Z * q.Z);
        var roll = MathF.Atan2(sinrCosp, cosrCosp);

        return (yaw * 180f / MathF.PI, pitch * 180f / MathF.PI, roll * 180f / MathF.PI);
    }

    /// <summary>
    /// Converts a yaw angle in degrees to a cardinal or intercardinal direction.
    /// </summary>
    /// <param name="yaw">The yaw angle in degrees (0 = North, negative = clockwise towards East, positive = counter-clockwise towards West).</param>
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