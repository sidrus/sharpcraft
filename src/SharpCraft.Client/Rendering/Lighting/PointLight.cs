using System.Numerics;

namespace SharpCraft.Client.Rendering.Lighting;

public class PointLight : ILight
{
    public Vector3 Color { get; set; } = Vector3.One;
    public float Intensity { get; set; } = 1.0f;
    public bool IsEnabled { get; set; } = true;
    public Vector3 Position { get; set; }

    public float Constant { get; set; } = 1.0f;
    public float Linear { get; set; } = 0.09f;
    public float Quadratic { get; set; } = 0.032f;
}