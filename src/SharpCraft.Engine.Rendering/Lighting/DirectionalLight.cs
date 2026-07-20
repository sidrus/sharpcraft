using System.Numerics;

namespace SharpCraft.Engine.Rendering.Lighting;

public class DirectionalLight : IDirectionalLight
{
    public Vector3 Color { get; set; } = Vector3.One;
    public float Intensity { get; set; } = 1.0f;
    public Vector3 Direction { get; set; } = new(0.5f, -1.0f, 0.5f);
}