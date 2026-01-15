using SharpCraft.Sdk.Blocks;

namespace SharpCraft.Sdk.Physics.Sensors.Spatial;

public class SpatialSensor : ISensor<SpatialSensorData>
{
    public SpatialSensorData? LastSense { get; private set; }

    public SpatialSensorData Sense(ICollisionProvider world, IPhysicsEntity entity)
    {
        return LastSense = CreateData(world, entity);
    }

    protected virtual SpatialSensorData CreateData(ICollisionProvider collisionProvider, IPhysicsEntity entity)
    {
        var data = CreateBaseData(collisionProvider, entity);
        return PopulateData(data, collisionProvider, entity);
    }

    protected virtual SpatialSensorData CreateBaseData(ICollisionProvider collisionProvider, IPhysicsEntity entity) => new();

    protected static SpatialSensorData PopulateData(SpatialSensorData data, ICollisionProvider collisionProvider, IPhysicsEntity entity)
    {
        var pos = entity.Position;
        var footY = (int)Math.Floor(pos.Y - 0.1f);

        // Small offset below feet
        var blockBelow = collisionProvider.GetBlock(
            (int)Math.Floor(pos.X),
            footY,
            (int)Math.Floor(pos.Z));

        // Head level
        var blockAbove = collisionProvider.GetBlock(
            (int)Math.Floor(pos.X),
            (int)Math.Floor(pos.Y + entity.Size.Y - 0.2f),
            (int)Math.Floor(pos.Z));

        var blockAtMid = collisionProvider.GetBlock(
            (int)Math.Floor(pos.X),
            (int)Math.Floor(pos.Y + 0.5f),
            (int)Math.Floor(pos.Z));

        var isUnderwater = blockAbove.Type == BlockType.Water;
        var isSwimming = isUnderwater || blockAtMid.Type == BlockType.Water;
        var isOnWaterSurface = blockBelow.Type == BlockType.Water && !isSwimming;
        var isFlying = blockAbove.Type == BlockType.Air && blockBelow.Type == BlockType.Air;
        var isGrounded = blockBelow.IsSolid && blockAbove.Type == BlockType.Air;

        // Calculate SubmersionDepth (how deep the feet are into water)
        // Water surface is at floor(Y) + 1.0 if the block at floor(Y) is water.
        // If we are at Y=63.5 and Y=63 is water, depth = 64.0 - 63.5 = 0.5
        var currentBlockY = (int)Math.Floor(pos.Y);
        var blockAtFeet = collisionProvider.GetBlock((int)Math.Floor(pos.X), currentBlockY, (int)Math.Floor(pos.Z));

        var submersionDepth = 0f;
        if (blockAtFeet.Type == BlockType.Water)
        {
            submersionDepth = (currentBlockY + 1) - pos.Y;
        }
        else if (collisionProvider.GetBlock((int)Math.Floor(pos.X), currentBlockY - 1, (int)Math.Floor(pos.Z)).Type == BlockType.Water)
        {
            // If feet are just above water (e.g. at 64.04), depth is negative
            submersionDepth = currentBlockY - pos.Y;
        }
        else
        {
            submersionDepth = 0;
        }

        return data with
        {
            BlockBelow = blockBelow,
            BlockAbove = blockAbove,
            IsUnderwater = isUnderwater,
            IsSwimming = isSwimming,
            IsFlying = isFlying,
            IsGrounded = isGrounded,
            IsOnWaterSurface = isOnWaterSurface,
            SubmersionDepth = submersionDepth,
        };
    }
}