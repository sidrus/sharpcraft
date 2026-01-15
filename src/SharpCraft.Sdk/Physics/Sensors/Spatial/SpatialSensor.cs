using SharpCraft.Sdk.Blocks;

namespace SharpCraft.Sdk.Physics.Sensors.Spatial;

/// <summary>
/// A sensor that detects spatial information about an entity's surroundings, such as blocks above/below and fluid submersion.
/// </summary>
public class SpatialSensor : ISensor<SpatialSensorData>
{
    /// <inheritdoc />
    public SpatialSensorData? LastSense { get; private set; }

    /// <inheritdoc />
    public SpatialSensorData Sense(ICollisionProvider world, IPhysicsEntity entity)
    {
        return LastSense = CreateData(world, entity);
    }

    /// <summary>
    /// Creates the sensor data for the current state.
    /// </summary>
    /// <param name="world">The collision provider to sense from.</param>
    /// <param name="entity">The entity performing the sensing.</param>
    /// <returns>The populated sensor data.</returns>
    protected virtual SpatialSensorData CreateData(ICollisionProvider world, IPhysicsEntity entity)
    {
        var data = CreateBaseData(world, entity);
        return PopulateData(data, world, entity);
    }

    /// <summary>
    /// Creates the initial base data object.
    /// </summary>
    /// <param name="collisionProvider">The collision provider.</param>
    /// <param name="entity">The entity.</param>
    /// <returns>A new <see cref="SpatialSensorData"/> or derived instance.</returns>
    protected virtual SpatialSensorData CreateBaseData(ICollisionProvider collisionProvider, IPhysicsEntity entity) => new();

    /// <summary>
    /// Populates the spatial data based on the current world state.
    /// </summary>
    /// <param name="data">The data object to populate.</param>
    /// <param name="collisionProvider">The collision provider to sense from.</param>
    /// <param name="entity">The entity performing the sensing.</param>
    /// <returns>The populated data.</returns>
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

        var defBelow = collisionProvider.Blocks.Get(blockBelow.Id);
        var defAbove = collisionProvider.Blocks.Get(blockAbove.Id);
        var defMid = collisionProvider.Blocks.Get(blockAtMid.Id);

        var isUnderwater = blockAbove.Id == BlockIds.Water;
        var isSwimming = isUnderwater || blockAtMid.Id == BlockIds.Water;
        var isOnWaterSurface = blockBelow.Id == BlockIds.Water && !isSwimming;
        var isFlying = blockAbove.IsAir && blockBelow.IsAir;
        var isGrounded = defBelow.IsSolid && blockAbove.IsAir;

        // Calculate SubmersionDepth (how deep the feet are into water)
        // Water surface is at floor(Y) + 1.0 if the block at floor(Y) is water.
        // If we are at Y=63.5 and Y=63 is water, depth = 64.0 - 63.5 = 0.5
        var currentBlockY = (int)Math.Floor(pos.Y);
        var blockAtFeet = collisionProvider.GetBlock((int)Math.Floor(pos.X), currentBlockY, (int)Math.Floor(pos.Z));

        var submersionDepth = 0f;
        if (blockAtFeet.Id == BlockIds.Water)
        {
            submersionDepth = (currentBlockY + 1) - pos.Y;
        }
        else if (collisionProvider.GetBlock((int)Math.Floor(pos.X), currentBlockY - 1, (int)Math.Floor(pos.Z)).Id == BlockIds.Water)
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
            BelowFriction = defBelow.Friction,
            BelowIsSolid = defBelow.IsSolid,
            IsUnderwater = isUnderwater,
            IsSwimming = isSwimming,
            IsFlying = isFlying,
            IsGrounded = isGrounded,
            IsOnWaterSurface = isOnWaterSurface,
            SubmersionDepth = submersionDepth,
        };
    }
}