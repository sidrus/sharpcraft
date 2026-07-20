namespace SharpCraft.Sdk.Blocks;

/// <summary>
/// A fluid's physical behavior as data. The engine's fluid motor reads it generically, so water
/// and lava differ only by these numbers.
/// </summary>
public record FluidProperties(
    float Density,
    float Friction,
    float BuoyantGravity,
    float SwimUpVelocity,
    float DeepSwimUpVelocity,
    float DeepSwimDepth,
    float SwimDownVelocity,
    float SpeedMultiplier
);