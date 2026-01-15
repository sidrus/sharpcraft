using System.Numerics;
using SharpCraft.Sdk.Physics;

namespace SharpCraft.Engine.Physics;

/// <summary>
/// Represents an entity that is affected by physics.
/// </summary>
public sealed class PhysicsEntity(Transform transform, IPhysicsSystem physics) : IPhysicsEntity
{
    private Transform _transform = transform;
    private Vector3 _prevPosition = transform.Position;
    private Quaternion _prevRotation = transform.Rotation;

    public Transform Transform => _transform;

    /// <inheritdoc />
    public Quaternion Rotation
    {
        get => _transform.Rotation;
        set => _transform.Rotation = value;
    }

    /// <inheritdoc />
    public Vector3 Velocity { get; set; } = Vector3.Zero;

    /// <inheritdoc />
    public Vector3 Size { get; } = new Vector3(0.6f, 1.8f, 0.6f) * transform.Scale;

    /// <inheritdoc />
    public Vector3 Position => _transform.Position;

    /// <inheritdoc />
    public void SetPosition(Vector3 position)
    {
        _transform.Position = position;
        _prevPosition = position;
        Velocity = Vector3.Zero;
    }

    /// <inheritdoc />
    public Vector3 PreviousPosition => _prevPosition;

    /// <inheritdoc />
    public Quaternion PreviousRotation => _prevRotation;

    /// <inheritdoc />
    public bool IsGrounded { get; private set; }

    /// <inheritdoc />
    public Vector3 Forward => Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, Rotation));

    /// <inheritdoc />
    public Vector3 Right => Vector3.Normalize(Vector3.Transform(Vector3.UnitX, Rotation));

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        _prevPosition = _transform.Position;
        _prevRotation = _transform.Rotation;

        var oldPos = _transform.Position;
        var movement = Velocity * deltaTime;

        _transform.Position = physics.MoveAndResolve(oldPos, movement, Size);

        // Reset velocity for axes that were blocked by a wall/floor
        var actualMovement = _transform.Position - oldPos;
        var newVelocity = Velocity;
        if (MathF.Abs(actualMovement.X - movement.X) > PhysicsConstants.Epsilon) newVelocity.X = 0;
        if (MathF.Abs(actualMovement.Y - movement.Y) > PhysicsConstants.Epsilon) newVelocity.Y = 0;
        if (MathF.Abs(actualMovement.Z - movement.Z) > PhysicsConstants.Epsilon) newVelocity.Z = 0;
        Velocity = newVelocity;

        IsGrounded = actualMovement.Y > movement.Y + (PhysicsConstants.Epsilon * 0.1f) && movement.Y <= 0;
    }
}
