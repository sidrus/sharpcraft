namespace SharpCraft.Sdk.Physics;

/// <summary>
/// Represents a change in look direction.
/// </summary>
/// <param name="Yaw">The change in yaw (horizontal rotation) in degrees.</param>
/// <param name="Pitch">The change in pitch (vertical rotation) in degrees.</param>
public readonly record struct LookDelta(float Yaw, float Pitch);
