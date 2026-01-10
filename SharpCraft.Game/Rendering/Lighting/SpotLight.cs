using System.Numerics;

namespace SharpCraft.Game.Rendering.Lighting;

public class SpotLight : ILight
{
    public Vector3 Color { get; set; } = Vector3.One;
    public float Intensity { get; set; } = 1.0f;
    public bool IsEnabled { get; set; } = true;
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; }
    public float CutOff { get; set; } = MathF.Cos(12.5f * MathF.PI / 180f);
    public float OuterCutOff { get; set; } = MathF.Cos(15.0f * MathF.PI / 180f);
}