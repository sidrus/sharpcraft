using System.Numerics;

namespace SharpCraft.Core.Physics;

/// <summary>
/// Implements axis-by-axis collision resolution.
/// </summary>
public sealed class PhysicsSystem(ICollisionProvider world) : IPhysicsSystem
{
    /// <inheritdoc />
    public Vector3 MoveAndResolve(Vector3 position, Vector3 velocity, Vector3 size)
    {
        // Move X and resolve
        position.X += velocity.X;
        position = ResolveAxis(position, size, 0);

        // Move Y and resolve
        position.Y += velocity.Y;
        position = ResolveAxis(position, size, 1);

        // Move Z and resolve
        position.Z += velocity.Z;
        position = ResolveAxis(position, size, 2);

        return position;
    }

    private Vector3 ResolveAxis(Vector3 position, Vector3 size, int axis)
    {
        var entityBox = AABB.FromPositionSize(position, size);

        var minX = (int)Math.Floor(entityBox.Min.X);
        var maxX = (int)Math.Floor(entityBox.Max.X);
        var minY = (int)Math.Floor(entityBox.Min.Y);
        var maxY = (int)Math.Floor(entityBox.Max.Y);
        var minZ = (int)Math.Floor(entityBox.Min.Z);
        var maxZ = (int)Math.Floor(entityBox.Max.Z);

        for (var x = minX; x <= maxX; x++)
            for (var y = minY; y <= maxY; y++)
                for (var z = minZ; z <= maxZ; z++)
                    if (world.GetBlock(x, y, z).IsSolid)
                    {
                        var blockBox = new AABB(new Vector3(x, y, z), new Vector3(x + 1, y + 1, z + 1));
                        if (entityBox.Intersects(blockBox))
                        {
                            position = PushOut(position, entityBox, blockBox, axis);
                            entityBox = AABB.FromPositionSize(position, size);
                        }
                    }

        return position;
    }

    private static Vector3 PushOut(Vector3 pos, AABB entity, AABB block, int axis)
    {
        if (axis == 0) // X Axis
        {
            // Determine which side of the block we hit and snap to it
            var centerE = (entity.Min.X + entity.Max.X) / 2;
            var centerB = (block.Min.X + block.Max.X) / 2;
            var halfWidth = (entity.Max.X - entity.Min.X) / 2;

            if (centerE < centerB)
            {
                pos.X = block.Min.X - halfWidth - 0.001f; // Snap to West side
            }
            else
            {
                pos.X = block.Max.X + halfWidth + 0.001f; // Snap to East side
            }
        }
        else if (axis == 1) // Y Axis
        {
            // Handle floor/ceiling
            if (entity.Min.Y < block.Min.Y)
            {
                pos.Y = block.Min.Y - (entity.Max.Y - entity.Min.Y) - 0.001f; // Snap to ceiling
            }
            else
            {
                pos.Y = block.Max.Y + 0.001f; // Snap to floor
            }
        }
        else // Z Axis
        {
            var centerE = (entity.Min.Z + entity.Max.Z) / 2;
            var centerB = (block.Min.Z + block.Max.Z) / 2;
            var halfDepth = (entity.Max.Z - entity.Min.Z) / 2;

            if (centerE < centerB)
            {
                pos.Z = block.Min.Z - halfDepth - 0.001f; // Snap to North side
            }
            else
            {
                pos.Z = block.Max.Z + halfDepth + 0.001f; // Snap to South side
            }
        }

        return pos;
    }
}