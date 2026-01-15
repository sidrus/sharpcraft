using System.Numerics;

namespace SharpCraft.Sdk.Physics.Collision;

/// <summary>
/// Represents an Axis-Aligned Bounding Box (AABB) for collision detection.
/// </summary>
public record struct AABB(Vector3 min, Vector3 max)
{
    /// <summary>
    /// Gets the minimum corner of the bounding box.
    /// </summary>
    public Vector3 Min => min;

    /// <summary>
    /// Gets the maximum corner of the bounding box.
    /// </summary>
    public Vector3 Max => max;

    /// <summary>
    /// Gets the center of the bounding box.
    /// </summary>
    public Vector3 Center => (Min + Max) * 0.5f;

    /// <summary>
    /// Gets the size of the bounding box.
    /// </summary>
    public Vector3 Size => Max - Min;

    /// <summary>
    /// Gets the half-extents of the bounding box.
    /// </summary>
    public Vector3 Extents => Size * 0.5f;

    /// <summary>
    /// Checks if this AABB intersects with another AABB.
    /// </summary>
    /// <param name="other">The other bounding box.</param>
    /// <returns>True if they intersect, otherwise false.</returns>
    public bool Intersects(AABB other) =>
        Min.X < other.Max.X && Max.X > other.Min.X &&
        Min.Y < other.Max.Y && Max.Y > other.Min.Y &&
        Min.Z < other.Max.Z && Max.Z > other.Min.Z;

    /// <summary>
    /// Creates an AABB from a center position and size.
    /// </summary>
    /// <param name="position">The center position (X/Z) and bottom position (Y).</param>
    /// <param name="size">The dimensions of the box.</param>
    /// <returns>A new AABB.</returns>
    public static AABB FromPositionSize(Vector3 position, Vector3 size) =>
        new(position - new Vector3(size.X / 2, 0, size.Z / 2),
            position + new Vector3(size.X / 2, size.Y, size.Z / 2));
}
