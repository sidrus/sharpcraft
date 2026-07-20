using System.Numerics;

namespace SharpCraft.Sdk.Physics;

/// <summary>
/// Represents the position, rotation, and scale of an object.
/// </summary>
public readonly record struct Transform
{
    /// <summary>
    /// The position in 3D space.
    /// </summary>
    public Vector3 Position { get; init; } = Vector3.Zero;

    /// <summary>
    /// The rotation as a quaternion.
    /// </summary>
    public Quaternion Rotation { get; init; } = Quaternion.Identity;

    /// <summary>
    /// The scale factor.
    /// </summary>
    public Vector3 Scale { get; init; } = Vector3.One;

    /// <summary>
    /// Initializes a new instance of the <see cref="Transform"/> struct with default values.
    /// </summary>
    public Transform()
    {
    }
}