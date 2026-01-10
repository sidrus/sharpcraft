using System.Numerics;
using SharpCraft.Core.Physics;

namespace SharpCraft.Game.Rendering.Cameras;

public class FirstPersonCamera(PhysicsEntity parent, Vector3 offset) : ICamera
{
    public float Pitch { get; set; } = 0;
    public float Zoom { get; set; } = 60f;
    public Vector3 Position => parent.Position + offset;

    public Vector3 Forward
    {
        get
        {
            var pitchRad = Pitch * MathF.PI / 180f;
            var pitchRotation = Quaternion.CreateFromAxisAngle(parent.Right, pitchRad);
            return Vector3.Transform(parent.Forward, pitchRotation);
        }
    }

    public Vector3 Up => Vector3.UnitY;

    public Matrix4x4 GetViewMatrix() => Matrix4x4.CreateLookAt(Position, Position + Forward, Up);

    public Matrix4x4 GetProjectionMatrix(float aspect) => Matrix4x4.CreatePerspectiveFieldOfView(Zoom * MathF.PI / 180f, aspect, 0.1f, 1000f);

    public void HandleMouse(float xOffset, float yOffset)
    {
        Pitch = Math.Clamp(Pitch + yOffset, -89f, 89f);
    }
}