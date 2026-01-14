using System.Numerics;

namespace SharpCraft.Client.Rendering.Cameras;

public interface ICamera
{
    public Vector3 Forward { get; }
    public Vector3 Up { get; }

    public Vector3 Position { get; }

    public Matrix4x4 GetViewMatrix(float alpha = 1.0f);
    public Matrix4x4 GetProjectionMatrix(float aspect);
    public void HandleMouse(float xOffset, float yOffset);
}