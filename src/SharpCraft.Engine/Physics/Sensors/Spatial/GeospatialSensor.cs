using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Numerics;
using SharpCraft.Sdk.Physics;

namespace SharpCraft.Engine.Physics.Sensors.Spatial;

/// <summary>
/// Detects spatial information about an entity's surroundings (blocks above/below, fluid
/// submersion, grounded/flying state) together with its geospatial orientation
/// (heading and pitch).
/// </summary>
public class GeospatialSensor
{
    /// <summary>
    /// Gets the most recent data captured by the sensor.
    /// </summary>
    public GeospatialSensorData? LastSense { get; private set; }

    /// <summary>
    /// Performs a sensing operation in the specified world for the given entity.
    /// </summary>
    /// <param name="world">The collision provider to sense from.</param>
    /// <param name="entity">The entity performing the sensing.</param>
    /// <returns>The captured sensor data.</returns>
    public GeospatialSensorData Sense(ICollisionProvider world, IPhysicsEntity entity)
    {
        var (yaw, pitch, _) = MathUtils.ToEulerAngles(entity.Rotation);
        var data = new GeospatialSensorData { Heading = yaw, Pitch = pitch };
        return LastSense = Populate(data, world, entity);
    }

    private static GeospatialSensorData Populate(GeospatialSensorData data, ICollisionProvider collisionProvider, IPhysicsEntity entity)
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

        var isSubmerged = blockAbove.IsFluid;
        var isSwimming = isSubmerged || blockAtMid.IsFluid;
        var isOnFluidSurface = blockBelow.IsFluid && !isSwimming;
        var isFlying = blockAbove.IsAir && blockBelow.IsAir;
        var isGrounded = blockBelow.IsSolid && blockAbove.IsAir;

        // Calculate SubmersionDepth (how deep the feet are into water)
        // Water surface is at floor(Y) + 1.0 if the block at floor(Y) is water.
        // If we are at Y=63.5 and Y=63 is water, depth = 64.0 - 63.5 = 0.5
        var currentBlockY = (int)Math.Floor(pos.Y);
        var blockAtFeet = collisionProvider.GetBlock((int)Math.Floor(pos.X), currentBlockY, (int)Math.Floor(pos.Z));

        var submersionDepth = 0f;
        if (blockAtFeet.IsFluid)
        {
            submersionDepth = (currentBlockY + 1) - pos.Y;
        }
        else if (collisionProvider.GetBlock((int)Math.Floor(pos.X), currentBlockY - 1, (int)Math.Floor(pos.Z)).IsFluid)
        {
            // If feet are just above water (e.g. at 64.04), depth is negative
            submersionDepth = currentBlockY - pos.Y;
        }
        else
        {
            submersionDepth = 0;
        }

        // A climbable ledge is a solid horizontal neighbour at foot level with open
        // space above it to stand in. This lets the player hop out of the water onto
        // land, while open water (no such neighbour) still can't be walked on.
        var isNextToClimbableLedge = false;
        if (isOnFluidSurface)
        {
            var blockX = (int)Math.Floor(pos.X);
            var blockZ = (int)Math.Floor(pos.Z);

            foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                var ledge = collisionProvider.GetBlock(blockX + dx, footY, blockZ + dz);
                var aboveLedge = collisionProvider.GetBlock(blockX + dx, footY + 1, blockZ + dz);
                if (ledge.IsSolid && !aboveLedge.IsSolid)
                {
                    isNextToClimbableLedge = true;
                    break;
                }
            }
        }

        return data with
        {
            BlockBelow = blockBelow,
            BlockAbove = blockAbove,
            BlockAtMid = blockAtMid,
            IsSubmerged = isSubmerged,
            IsSwimming = isSwimming,
            IsFlying = isFlying,
            IsGrounded = isGrounded,
            IsOnFluidSurface = isOnFluidSurface,
            IsNextToClimbableLedge = isNextToClimbableLedge,
            SubmersionDepth = submersionDepth,
        };
    }
}
