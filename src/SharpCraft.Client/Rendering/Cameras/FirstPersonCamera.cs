using System.Numerics;
using SharpCraft.Engine.Physics;

namespace SharpCraft.Client.Rendering.Cameras;

public class FirstPersonCamera(PhysicsEntity parent, Vector3 offset) : ICamera
{
    public float Pitch { get; set; } = 0;
    public float Zoom { get; set; } = 60f;
    public Vector3 Position => parent.Position + offset;
    public Vector3 GetInterpolatedPosition(float alpha) => Vector3.Lerp(parent.PreviousPosition, parent.Position, alpha) + offset;

    public Vector3 Forward => GetForward(1.0f);
    public Vector3 Right => GetRight(1.0f);

    public Vector3 GetForward(float alpha)
    {
        var pitchRad = Pitch * MathF.PI / 180f;
        var rotation = Quaternion.Lerp(parent.PreviousRotation, parent.Rotation, alpha);
        var right = Vector3.Normalize(Vector3.Transform(Vector3.UnitX, rotation));
        var forward = Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, rotation));
        var pitchRotation = Quaternion.CreateFromAxisAngle(right, pitchRad);
        return Vector3.Transform(forward, pitchRotation);
    }

    public Vector3 GetRight(float alpha)
    {
        var rotation = Quaternion.Lerp(parent.PreviousRotation, parent.Rotation, alpha);
        return Vector3.Normalize(Vector3.Transform(Vector3.UnitX, rotation));
    }

    public Vector3 Up => Vector3.UnitY;

    public Matrix4x4 GetViewMatrix(float alpha = 1.0f)
    {
        var pos = GetInterpolatedPosition(alpha);
        var fwd = GetForward(alpha);
        return Matrix4x4.CreateLookAt(pos, pos + fwd, Up);
    }

    public Matrix4x4 GetProjectionMatrix(float aspect) => Matrix4x4.CreatePerspectiveFieldOfView(Zoom * MathF.PI / 180f, aspect, 0.1f, 1000f);

    public void HandleMouse(float xOffset, float yOffset)
    {
        Pitch = Math.Clamp(Pitch + yOffset, -89f, 89f);
    }
}