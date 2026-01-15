using System.Numerics;
using SharpCraft.Sdk.Physics;

namespace SharpCraft.Sdk.Input;

/// <summary>
/// Provides input for controlling an entity.
/// </summary>
public interface IInputProvider
{
    /// <summary>
    /// Gets the movement intent based on the current input state.
    /// </summary>
    /// <param name="forward">The forward direction of the entity.</param>
    /// <param name="right">The right direction of the entity.</param>
    /// <returns>A <see cref="MovementIntent"/> representing the desired movement.</returns>
    MovementIntent GetMovementIntent(Vector3 forward, Vector3 right);

    /// <summary>
    /// Gets the look delta based on the current input state.
    /// </summary>
    /// <returns>A <see cref="LookDelta"/> representing the change in look direction.</returns>
    LookDelta GetLookDelta();
}
