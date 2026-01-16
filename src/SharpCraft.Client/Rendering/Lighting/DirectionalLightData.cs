using System.Numerics;

namespace SharpCraft.Client.Rendering.Lighting;

public record struct DirectionalLightData(
    Vector3 Direction,
    Vector3 Color,
    float Intensity = 1.0f
);
