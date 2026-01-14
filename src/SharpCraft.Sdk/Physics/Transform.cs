using System.Numerics;

namespace SharpCraft.Sdk.Physics;

/// <summary>
/// Represents the position, rotation, and scale of an object.
/// </summary>
public struct Transform
{
    /// <summary>
    /// The position in 3D space.
    /// </summary>
    public Vector3 Position = Vector3.Zero;

    /// <summary>
    /// The rotation as a quaternion.
    /// </summary>
    public Quaternion Rotation = Quaternion.Identity;

    /// <summary>
    /// The scale factor.
    /// </summary>
    public Vector3 Scale = Vector3.One;

    /// <summary>
    /// Initializes a new instance of the <see cref="Transform"/> struct with default values.
    /// </summary>
    public Transform() {}
}
