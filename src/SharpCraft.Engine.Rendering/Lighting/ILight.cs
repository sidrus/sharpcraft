using System.Numerics;

namespace SharpCraft.Engine.Rendering.Lighting;

public interface ILight
{
    Vector3 Color { get; set; }
    float Intensity { get; set; }
    bool IsEnabled { get; set; }
}