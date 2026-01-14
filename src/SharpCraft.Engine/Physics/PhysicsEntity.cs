using System.Numerics;
using SharpCraft.Sdk.Physics;

namespace SharpCraft.Engine.Physics;

/// <summary>
/// Represents an entity that is affected by physics.
/// </summary>
public sealed class PhysicsEntity(Transform transform, IPhysicsSystem physics)
{
    private Transform _transform = transform;
    private Vector3 _prevPosition = transform.Position;
    private Quaternion _prevRotation = transform.Rotation;

    /// <summary>
    /// Gets or sets the rotation of the entity.
    /// </summary>
    public Quaternion Rotation
    {
        get => _transform.Rotation;
        set => _transform.Rotation = value;
    }

    /// <summary>
    /// The current velocity of the entity.
    /// </summary>
    public Vector3 Velocity = Vector3.Zero;

    /// <summary>
    /// The size of the entity's bounding box.
    /// </summary>
    public Vector3 Size = new Vector3(0.6f, 1.8f, 0.6f) * transform.Scale;

    /// <summary>
    /// Gets the current position of the entity.
    /// </summary>
    public Vector3 Position => _transform.Position;

    /// <summary>
    /// Sets the position of the entity.
    /// </summary>
    /// <param name="position">The new position.</param>
    public void SetPosition(Vector3 position)
    {
        _transform.Position = position;
        _prevPosition = position;
        Velocity = Vector3.Zero;
    }

    /// <summary>
    /// Gets the position from the previous update.
    /// </summary>
    public Vector3 PreviousPosition => _prevPosition;

    /// <summary>
    /// Gets the rotation from the previous update.
    /// </summary>
    public Quaternion PreviousRotation => _prevRotation;

    /// <summary>
    /// Gets a value indicating whether the entity is currently on the ground.
    /// </summary>
    public bool IsGrounded { get; private set; }

    /// <summary>
    /// Gets the forward direction vector based on the entity's rotation.
    /// </summary>
    public Vector3 Forward => Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, Rotation));

    /// <summary>
    /// Gets the right direction vector based on the entity's rotation.
    /// </summary>
    public Vector3 Right => Vector3.Normalize(Vector3.Transform(Vector3.UnitX, Rotation));

    /// <summary>
    /// Updates the entity's physics state.
    /// </summary>
    /// <param name="deltaTime">The time since the last update.</param>
    public void Update(float deltaTime)
    {
        _prevPosition = _transform.Position;
        _prevRotation = _transform.Rotation;

        var oldPos = _transform.Position;
        var movement = Velocity * deltaTime;

        _transform.Position = physics.MoveAndResolve(oldPos, movement, Size);

        // Reset velocity for axes that were blocked by a wall/floor
        var actualMovement = _transform.Position - oldPos;
        if (MathF.Abs(actualMovement.X - movement.X) > PhysicsConstants.Epsilon) Velocity.X = 0;
        if (MathF.Abs(actualMovement.Y - movement.Y) > PhysicsConstants.Epsilon) Velocity.Y = 0;
        if (MathF.Abs(actualMovement.Z - movement.Z) > PhysicsConstants.Epsilon) Velocity.Z = 0;

        IsGrounded = actualMovement.Y > movement.Y + (PhysicsConstants.Epsilon * 0.1f) && movement.Y <= 0;
    }
}
