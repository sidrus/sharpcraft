using System.Numerics;
using SharpCraft.Client.Controllers;
using SharpCraft.Client.Rendering.Cameras;
using SharpCraft.Engine.Physics;
using SharpCraft.Engine.World;
using SharpCraft.Sdk.Physics;
using SharpCraft.Engine.Blocks;
using Silk.NET.Input;
using Moq;
using AwesomeAssertions;

namespace SharpCraft.Client.Tests.Controllers;

public class LocalPlayerControllerTests
{
    [Fact]
    public void Update_WhenHoldingSpaceOnWaterSurface_ShouldEventuallySubmergeDeeply()
    {
        // Setup
        var world = new World();
        // Water at Y=63 (occupies 63.0 to 64.0)
        for (var x = -5; x <= 5; x++)
        {
            for (var z = -5; z <= 5; z++)
            {
                world.SetBlock(x, 63, z, BlockType.Water);
            }
        }

        var mockCamera = new Mock<ICamera>();
        var mockPhysicsSystem = new Mock<IPhysicsSystem>();
        
        // Entity starts at Y=64.0 (feet at water surface)
        var transform = new Transform { Position = new Vector3(0, 64.0f, 0) };
        var entity = new PhysicsEntity(transform, mockPhysicsSystem.Object);
        var controller = new LocalPlayerController(entity, mockCamera.Object, world);

        var mockKeyboard = new Mock<IKeyboard>();
        mockKeyboard.Setup(k => k.IsKeyPressed(Key.Space)).Returns(true);

        // Mock MoveAndResolve to just apply movement
        mockPhysicsSystem.Setup(p => p.MoveAndResolve(It.IsAny<Vector3>(), It.IsAny<Vector3>(), It.IsAny<Vector3>()))
            .Returns((Vector3 pos, Vector3 move, Vector3 size) => pos + move);

        // Run several updates
        var deltaTime = 0.016f;
        for (var i = 0; i < 500; i++)
        {
            controller.Update(deltaTime, mockKeyboard.Object);
        }

        // Verify the player is submerged (not walking on top of water)
        // They should stay below 63.8 (at least 0.2m submerged)
        // Previously we required 0.9m but that prevented effective swimming.
        entity.Position.Y.Should().BeLessThan(63.8f);
    }

    [Fact]
    public void Update_WhenHoldingSpaceAtShallowSwimmingDepth_ShouldNotSink()
    {
        // Setup
        var world = new World();
        // Water at Y=63 (occupies 63.0 to 64.0)
        for (var x = -5; x <= 5; x++)
        {
            for (var z = -5; z <= 5; z++)
            {
                world.SetBlock(x, 63, z, BlockType.Water);
            }
        }

        var mockCamera = new Mock<ICamera>();
        var mockPhysicsSystem = new Mock<IPhysicsSystem>();
        
        // Entity starts at Y=63.0 (SubmersionDepth = 1.0m)
        // At this depth, IsSwimming is true, but SubmersionDepth <= 1.1f
        var transform = new Transform { Position = new Vector3(0, 63.0f, 0) };
        var entity = new PhysicsEntity(transform, mockPhysicsSystem.Object);
        var controller = new LocalPlayerController(entity, mockCamera.Object, world);

        var mockKeyboard = new Mock<IKeyboard>();
        mockKeyboard.Setup(k => k.IsKeyPressed(Key.Space)).Returns(true);

        mockPhysicsSystem.Setup(p => p.MoveAndResolve(It.IsAny<Vector3>(), It.IsAny<Vector3>(), It.IsAny<Vector3>()))
            .Returns((Vector3 pos, Vector3 move, Vector3 size) => pos + move);

        // Run one update
        controller.Update(0.016f, mockKeyboard.Object);

        // Should NOT have sunk (Y should be >= 63.0 or even > 63.0)
        // Currently, it will sink because no upward force is applied at 1.0m depth
        entity.Position.Y.Should().BeGreaterThanOrEqualTo(63.0f);
    }

    [Fact]
    public void Update_WhenJumpingFromDepth_ShouldBeAbleToReachAboveSurface()
    {
        // Setup
        var world = new World();
        for (var x = -5; x <= 5; x++)
        {
            for (var z = -5; z <= 5; z++)
            {
                world.SetBlock(x, 63, z, BlockType.Water);
            }
        }

        var mockCamera = new Mock<ICamera>();
        var mockPhysicsSystem = new Mock<IPhysicsSystem>();
        
        // Entity starts at Y=62.5 (SubmersionDepth = 1.5m)
        var transform = new Transform { Position = new Vector3(0, 62.5f, 0) };
        var entity = new PhysicsEntity(transform, mockPhysicsSystem.Object);
        var controller = new LocalPlayerController(entity, mockCamera.Object, world);

        var mockKeyboard = new Mock<IKeyboard>();
        mockKeyboard.Setup(k => k.IsKeyPressed(Key.Space)).Returns(true);

        mockPhysicsSystem.Setup(p => p.MoveAndResolve(It.IsAny<Vector3>(), It.IsAny<Vector3>(), It.IsAny<Vector3>()))
            .Returns((Vector3 pos, Vector3 move, Vector3 size) => pos + move);

        // Run updates until we reach peak height or timeout
        var maxObservedY = 62.5f;
        for (var i = 0; i < 100; i++)
        {
            controller.Update(0.016f, mockKeyboard.Object);
            if (entity.Position.Y > maxObservedY) maxObservedY = entity.Position.Y;
        }

        // Peak Y should be above 64.0 (surface) to allow jumping out onto land
        maxObservedY.Should().BeGreaterThan(64.0f);
    }
}
