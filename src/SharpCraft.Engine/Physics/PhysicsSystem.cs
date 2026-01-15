using System.Numerics;
using SharpCraft.Sdk.Physics;
using SharpCraft.Sdk.Physics.Collision;

namespace SharpCraft.Engine.Physics;

/// <summary>
/// Implements the core physics movement and collision resolution logic.
/// </summary>
public sealed class PhysicsSystem(ICollisionProvider world) : IPhysicsSystem
{
    /// <inheritdoc />
    public float Gravity { get; set; } = PhysicsConstants.DefaultGravity;

    /// <inheritdoc />
    public Vector3 MoveAndResolve(Vector3 position, Vector3 velocity, Vector3 size)
    {
        var nextPos = position;

        // X Axis
        if (MathF.Abs(velocity.X) > 0)
        {
            nextPos.X += velocity.X;
            if (HasCollision(nextPos, size))
            {
                var halfSize = size.X * 0.5f;
                nextPos.X = velocity.X > 0
                    ? MathF.Floor(nextPos.X + halfSize) - halfSize - PhysicsConstants.Epsilon
                    : MathF.Ceiling(nextPos.X - halfSize) + halfSize + PhysicsConstants.Epsilon;
            }
        }

        // Z Axis
        if (MathF.Abs(velocity.Z) > 0)
        {
            nextPos.Z += velocity.Z;
            if (HasCollision(nextPos, size))
            {
                var halfSize = size.Z * 0.5f;
                nextPos.Z = velocity.Z > 0
                    ? MathF.Floor(nextPos.Z + halfSize) - halfSize - PhysicsConstants.Epsilon
                    : MathF.Ceiling(nextPos.Z - halfSize) + halfSize + PhysicsConstants.Epsilon;
            }
        }

        // Y Axis (Vertical)
        if (MathF.Abs(velocity.Y) > 0)
        {
            nextPos.Y += velocity.Y;
            if (HasCollision(nextPos, size))
            {
                nextPos.Y = velocity.Y > 0
                    ? MathF.Floor(nextPos.Y + size.Y) - size.Y - PhysicsConstants.Epsilon
                    : MathF.Ceiling(nextPos.Y) + PhysicsConstants.Epsilon;
            }
        }

        return nextPos;
    }

    private bool HasCollision(Vector3 position, Vector3 size)
    {
        var entityAABB = AABB.FromPositionSize(position, size);

        var minX = (int)MathF.Floor(entityAABB.Min.X);
        var minY = (int)MathF.Floor(entityAABB.Min.Y);
        var minZ = (int)MathF.Floor(entityAABB.Min.Z);
        var maxX = (int)MathF.Floor(entityAABB.Max.X);
        var maxY = (int)MathF.Floor(entityAABB.Max.Y);
        var maxZ = (int)MathF.Floor(entityAABB.Max.Z);

        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    var block = world.GetBlock(x, y, z);
                    var def = world.Blocks.Get(block.Id);
                    if (def.IsSolid)
                    {
                        var blockAABB = new AABB(new Vector3(x, y, z), new Vector3(x + 1, y + 1, z + 1));
                        if (entityAABB.Intersects(blockAABB))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}
