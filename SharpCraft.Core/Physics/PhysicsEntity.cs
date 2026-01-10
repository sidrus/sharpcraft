using System.Numerics;

namespace SharpCraft.Core.Physics;

public sealed class PhysicsEntity(Transform transform, IPhysicsSystem physics)
{
    private Transform _transform = transform;

    public Quaternion Rotation
    {
        get => _transform.Rotation;
        set => _transform.Rotation = value;
    }

    public Vector3 Velocity = Vector3.Zero;

    public Vector3 Size = new Vector3(0.6f, 1.8f, 0.6f) * transform.Scale;

    public Vector3 Position => _transform.Position;

    public bool IsGrounded { get; private set; }

    public Vector3 Forward => Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, Rotation));

    public Vector3 Right => Vector3.Normalize(Vector3.Transform(Vector3.UnitX, Rotation));

    public void Update(float deltaTime)
    {
        var oldX = _transform.Position.X;
        var oldY = _transform.Position.Y;
        var oldZ = _transform.Position.Z;

        var preCollisionVelocity = Velocity;

        _transform.Position = physics.MoveAndResolve(_transform.Position, Velocity * deltaTime, Size);

        // Reset velocity for axes that were blocked by a wall/floor
        if (Math.Abs(_transform.Position.X - (oldX + preCollisionVelocity.X * deltaTime)) > 0.001f) Velocity.X = 0;
        if (Math.Abs(_transform.Position.Y - (oldY + preCollisionVelocity.Y * deltaTime)) > 0.001f) Velocity.Y = 0;
        if (Math.Abs(_transform.Position.Z - (oldZ + preCollisionVelocity.Z * deltaTime)) > 0.001f) Velocity.Z = 0;

        IsGrounded = _transform.Position.Y > (oldY + preCollisionVelocity.Y * deltaTime + 0.0001f) && preCollisionVelocity.Y <= 0;
    }
}