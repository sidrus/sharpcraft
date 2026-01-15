using SharpCraft.Sdk.Physics;

namespace SharpCraft.Sdk.Universe;

/// <summary>
/// Represents a player in the world.
/// </summary>
public interface IPlayer
{
    /// <summary>
    /// Gets the physics entity associated with the player.
    /// </summary>
    IPhysicsEntity Entity { get; }

    /// <summary>
    /// Gets the player's heading (compass direction).
    /// </summary>
    string Heading { get; }

    /// <summary>
    /// Gets the player's yaw.
    /// </summary>
    float Yaw { get; }

    /// <summary>
    /// Gets the normalized yaw (0-360).
    /// </summary>
    float NormalizedYaw { get; }

    /// <summary>
    /// Gets the player's pitch.
    /// </summary>
    float Pitch { get; }

    /// <summary>
    /// Gets whether the player is on the ground.
    /// </summary>
    bool IsGrounded { get; }

    /// <summary>
    /// Gets or sets whether the player is in fly mode.
    /// </summary>
    bool IsFlying { get; set; }

    /// <summary>
    /// Gets whether the player is swimming.
    /// </summary>
    bool IsSwimming { get; }

    /// <summary>
    /// Gets whether the player is underwater.
    /// </summary>
    bool IsUnderwater { get; }

    /// <summary>
    /// Gets the current friction acting on the player.
    /// </summary>
    float Friction { get; }
    
    // For DebugHud's "Standing on"
    // We might need a way to get block info in SDK
}
