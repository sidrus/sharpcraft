using System.Numerics;

namespace SharpCraft.Core.Physics;

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

        var oldX = _transform.Position.X;
        var oldY = _transform.Position.Y;
        var oldZ = _transform.Position.Z;

        var preCollisionVelocity = Velocity;

        _transform.Position = physics.MoveAndResolve(_transform.Position, Velocity * deltaTime, Size);

        // Reset velocity for axes that were blocked by a wall/floor
        if (Math.Abs(_transform.Position.X - (oldX + preCollisionVelocity.X * deltaTime)) > 0.001f) Velocity.X = 0;
        if (Math.Abs(_transform.Position.Y - (oldY + preCollisionVelocity.Y * deltaTime)) > 0.001f) Velocity.Y = 0;
        if (Math.Abs(_transform.Position.Z - (oldZ + preCollisionVelocity.Z * deltaTime)) > 0.001f) Velocity.Z = 0;

        IsGrounded = _transform.Position.Y > oldY + preCollisionVelocity.Y * deltaTime + 0.0001f && preCollisionVelocity.Y <= 0;
    }
}