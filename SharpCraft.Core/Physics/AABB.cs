using System.Numerics;

namespace SharpCraft.Core.Physics;

/// <summary>
/// Represents an Axis-Aligned Bounding Box (AABB), which is a geometric structure commonly
/// used in physics simulations and collision detection. It defines a rectangular prism
/// aligned with the coordinate axes using minimum and maximum corner points in 3D space.
/// </summary>
public record struct AABB(Vector3 min, Vector3 max)
{
    public Vector3 Min => min;
    public Vector3 Max => max;

    public bool Intersects(AABB other) =>
        Min.X < other.Max.X && Max.X > other.Min.X &&
        Min.Y < other.Max.Y && Max.Y > other.Min.Y &&
        Min.Z < other.Max.Z && Max.Z > other.Min.Z;

    public static AABB FromPositionSize(Vector3 position, Vector3 size) =>
        new(position - new Vector3(size.X / 2, 0, size.Z / 2),
            position + new Vector3(size.X / 2, size.Y, size.Z / 2));
}