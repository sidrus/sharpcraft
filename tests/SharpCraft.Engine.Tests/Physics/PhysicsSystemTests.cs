using System.Numerics;
using SharpCraft.Engine.Physics;
using AwesomeAssertions;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Physics;

namespace SharpCraft.Engine.Tests.Physics;

public class PhysicsSystemTests
{
    private class FakeCollisionProvider : ICollisionProvider
    {
        public Dictionary<Vector3, BlockType> Blocks = new();

        public Block GetBlock(int worldX, int worldY, int worldZ)
        {
            if (Blocks.TryGetValue(new Vector3(worldX, worldY, worldZ), out var type))
            {
                return new Block { Type = type };
            }
            return new Block { Type = BlockType.Air };
        }
    }

    [Fact]
    public void MoveAndResolve_ShouldMove_WhenNoCollision()
    {
        var world = new FakeCollisionProvider();
        var system = new PhysicsSystem(world);
        var startPos = new Vector3(10, 10, 10);
        var velocity = new Vector3(1, 0, 0);
        var size = new Vector3(0.6f, 1.8f, 0.6f);

        var endPos = system.MoveAndResolve(startPos, velocity, size);

        endPos.Should().Be(startPos + velocity);
    }

    [Fact]
    public void MoveAndResolve_ShouldSnapToWall_WhenCollidingOnX()
    {
        var world = new FakeCollisionProvider();
        world.Blocks[new Vector3(11, 10, 10)] = BlockType.Stone;
        var system = new PhysicsSystem(world);
        var startPos = new Vector3(10.5f, 10, 10.5f);
        var velocity = new Vector3(0.5f, 0, 0); // Should hit block at X=11
        var size = new Vector3(0.6f, 1.8f, 0.6f);

        var endPos = system.MoveAndResolve(startPos, velocity, size);

        // Entity box: MinX = 10.5 + 0.5 - 0.3 = 10.7, MaxX = 11.3
        // Block box: MinX = 11, MaxX = 12
        // Intersects. Should snap to West side of block (block.Min.X - halfWidth - 0.001f)
        // 11 - 0.3 - 0.001 = 10.699
        endPos.X.Should().BeApproximately(10.699f, 0.0001f);
    }

    [Fact]
    public void MoveAndResolve_ShouldSnapToFloor_WhenCollidingOnY()
    {
        var world = new FakeCollisionProvider();
        world.Blocks[new Vector3(10, 9, 10)] = BlockType.Stone;
        var system = new PhysicsSystem(world);
        var startPos = new Vector3(10.5f, 10.0f, 10.5f);
        var velocity = new Vector3(0, -0.1f, 0); // Moving down
        var size = new Vector3(0.6f, 1.8f, 0.6f);

        var endPos = system.MoveAndResolve(startPos, velocity, size);

        // Entity box: MinY = 10.0 - 0.1 = 9.9, MaxY = 11.7
        // Block box: MinY = 9, MaxY = 10
        // Intersects. Should snap to floor (block.Max.Y + 0.001f)
        // 10 + 0.001 = 10.001
        endPos.Y.Should().BeApproximately(10.001f, 0.0001f);
    }

    [Fact]
    public void MoveAndResolve_ShouldSnapToCeiling_WhenCollidingOnY()
    {
        var world = new FakeCollisionProvider();
        world.Blocks[new Vector3(10, 12, 10)] = BlockType.Stone;
        var system = new PhysicsSystem(world);
        var startPos = new Vector3(10.5f, 10.0f, 10.5f); // MaxY = 11.8. No overlap with block at Y=12.
        var velocity = new Vector3(0, 0.5f, 0); // Moving up. New MaxY = 12.3.
        var size = new Vector3(0.6f, 1.8f, 0.6f);

        var endPos = system.MoveAndResolve(startPos, velocity, size);

        // block.Min.Y = 12. height = 1.8.
        // Snap: 12 - 1.8 - 0.001 = 10.199
        endPos.Y.Should().BeApproximately(10.199f, 0.0001f);
    }
}
