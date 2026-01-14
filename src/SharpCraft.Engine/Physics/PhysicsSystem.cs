using System.Numerics;
using SharpCraft.Sdk.Physics;

namespace SharpCraft.Engine.Physics;

/// <summary>
/// Implements the core physics movement and collision resolution logic.
/// </summary>
public sealed class PhysicsSystem(ICollisionProvider world) : IPhysicsSystem
{
    private const float Epsilon = 0.001f;

    /// <inheritdoc />
    public Vector3 MoveAndResolve(Vector3 position, Vector3 velocity, Vector3 size)
    {
        var nextPos = position;

        // X Axis
        if (Math.Abs(velocity.X) > 0)
        {
            nextPos.X += velocity.X;
            if (HasCollision(nextPos, size))
            {
                nextPos.X = velocity.X > 0
                    ? MathF.Floor(nextPos.X + size.X / 2f) - size.X / 2f - Epsilon
                    : MathF.Ceiling(nextPos.X - size.X / 2f) + size.X / 2f + Epsilon;
            }
        }

        // Z Axis
        if (Math.Abs(velocity.Z) > 0)
        {
            nextPos.Z += velocity.Z;
            if (HasCollision(nextPos, size))
            {
                nextPos.Z = velocity.Z > 0
                    ? MathF.Floor(nextPos.Z + size.Z / 2f) - size.Z / 2f - Epsilon
                    : MathF.Ceiling(nextPos.Z - size.Z / 2f) + size.Z / 2f + Epsilon;
            }
        }

        // Y Axis (Vertical)
        if (Math.Abs(velocity.Y) > 0)
        {
            nextPos.Y += velocity.Y;
            if (HasCollision(nextPos, size))
            {
                nextPos.Y = velocity.Y > 0
                    ? MathF.Floor(nextPos.Y + size.Y) - size.Y - Epsilon
                    : MathF.Ceiling(nextPos.Y) + Epsilon;
            }
        }

        return nextPos;
    }

    private bool HasCollision(Vector3 position, Vector3 size)
    {
        var min = position - new Vector3(size.X / 2f, 0, size.Z / 2f);
        var max = position + new Vector3(size.X / 2f, size.Y, size.Z / 2f);

        var minX = (int)MathF.Floor(min.X);
        var minY = (int)MathF.Floor(min.Y);
        var minZ = (int)MathF.Floor(min.Z);
        var maxX = (int)MathF.Floor(max.X);
        var maxY = (int)MathF.Floor(max.Y);
        var maxZ = (int)MathF.Floor(max.Z);

        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    var block = world.GetBlock(x, y, z);
                    if (block.IsSolid)
                    {
                        var blockAABB = new AABB(new Vector3(x, y, z), new Vector3(x + 1, y + 1, z + 1));
                        var entityAABB = new AABB(min, max);

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
