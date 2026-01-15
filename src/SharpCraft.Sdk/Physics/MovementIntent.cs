using System.Numerics;

namespace SharpCraft.Sdk.Physics;

/// <summary>
/// Represents the intended movement from an input source.
/// </summary>
/// <param name="Direction">The desired movement direction.</param>
/// <param name="IsJumping">Whether the entity is trying to jump.</param>
/// <param name="IsDescending">Whether the entity is trying to descend (e.g., when flying or swimming).</param>
/// <param name="IsFlying">Whether the entity is in flying mode.</param>
public readonly record struct MovementIntent(
    Vector3 Direction,
    bool IsJumping,
    bool IsDescending,
    bool IsFlying
);
