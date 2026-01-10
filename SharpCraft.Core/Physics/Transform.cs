using System.Numerics;

namespace SharpCraft.Core.Physics;

public struct Transform
{
    public Vector3 Position = Vector3.Zero;
    public Quaternion Rotation = Quaternion.Identity;
    public Vector3 Scale = Vector3.One;

    public Transform() {}
}