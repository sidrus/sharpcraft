using System.Numerics;

namespace SharpCraft.Game.Rendering.Lighting;

public interface ILight
{
    Vector3 Color { get; set; }
    float Intensity { get; set; }
    bool IsEnabled { get; set; }
}