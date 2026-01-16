using System.Numerics;

namespace SharpCraft.Sdk.Rendering;

public interface IDirectionalLight
{
    Vector3 Color { get; set; }
    float Intensity { get; set; }
    bool IsEnabled { get; set; }
    Vector3 Direction { get; set; }
}
