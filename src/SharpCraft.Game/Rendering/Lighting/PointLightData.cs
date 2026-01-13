using System.Numerics;

namespace SharpCraft.Game.Rendering.Lighting;

public record struct PointLightData(
    Vector3 Position,
    Vector3 Color,
    float Intensity = 1.0f,
    float Constant = 1.0f,
    float Linear = 0.09f,
    float Quadratic = 0.032f,
    float Range = 20.0f
);