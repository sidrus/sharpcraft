using System.Numerics;

namespace SharpCraft.Sdk.Physics;

/// <summary>
/// Represents an entity that is affected by physics.
/// </summary>
public interface IPhysicsEntity
{
    /// <summary>
    /// Gets or sets the rotation of the entity.
    /// </summary>
    public Quaternion Rotation { get; set; }

    /// <summary>
    /// The current velocity of the entity.
    /// </summary>
    public Vector3 Velocity { get; set; }

    /// <summary>
    /// The size of the entity's bounding box.
    /// </summary>
    public Vector3 Size { get; }

    /// <summary>
    /// Gets the current position of the entity.
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    /// Gets the position from the previous update.
    /// </summary>
    public Vector3 PreviousPosition { get; }

    /// <summary>
    /// Gets the rotation from the previous update.
    /// </summary>
    public Quaternion PreviousRotation { get; }

    /// <summary>
    /// Gets a value indicating whether the entity is currently on the ground.
    /// </summary>
    public bool IsGrounded { get; }

    /// <summary>
    /// Gets the forward direction vector based on the entity's rotation.
    /// </summary>
    public Vector3 Forward { get; }

    /// <summary>
    /// Gets the right direction vector based on the entity's rotation.
    /// </summary>
    public Vector3 Right { get; }

    /// <summary>
    /// Sets the position of the entity.
    /// </summary>
    /// <param name="position">The new position.</param>
    public void SetPosition(Vector3 position);

    /// <summary>
    /// Updates the entity's physics state.
    /// </summary>
    /// <param name="deltaTime">The time since the last update.</param>
    public void Update(float deltaTime);
}
