using System.Numerics;

namespace SharpCraft.Client.Rendering.Lighting;

public class DirectionalLight : ILight
{
    public Vector3 Color { get; set; } = Vector3.One;
    public float Intensity { get; set; } = 1.0f;
    public bool IsEnabled { get; set; } = true;
    public Vector3 Direction { get; set; } = new(0.5f, -1.0f, 0.5f);
}