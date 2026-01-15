using System.Numerics;
using SharpCraft.Client.Controllers;
using SharpCraft.Client.Rendering.Cameras;
using SharpCraft.Engine.Physics;
using SharpCraft.Sdk.Physics;
using Moq;
using AwesomeAssertions;
using SharpCraft.Engine.Universe;
using SharpCraft.Sdk.Blocks;

using SharpCraft.Sdk.Input;

namespace SharpCraft.Client.Tests.Controllers;

public class LocalPlayerControllerTests
{
    private readonly IInputProvider _inputProvider = Mock.Of<IInputProvider>();

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
        var mockInput = new Mock<IInputProvider>();
        mockInput.Setup(i => i.GetMovementIntent(It.IsAny<Vector3>(), It.IsAny<Vector3>()))
            .Returns(new MovementIntent(Vector3.Zero, true, false, false));

        var controller = new LocalPlayerController(entity, mockCamera.Object, world, mockInput.Object);

        // Mock MoveAndResolve to just apply movement
        mockPhysicsSystem.Setup(p => p.MoveAndResolve(It.IsAny<Vector3>(), It.IsAny<Vector3>(), It.IsAny<Vector3>()))
            .Returns((Vector3 pos, Vector3 move, Vector3 size) => pos + move);

        // Run several updates
        var deltaTime = 0.016f;
        for (var i = 0; i < 500; i++)
        {
            controller.OnUpdate(deltaTime);
            controller.OnFixedUpdate(deltaTime);
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
        var mockInput = new Mock<IInputProvider>();
        mockInput.Setup(i => i.GetMovementIntent(It.IsAny<Vector3>(), It.IsAny<Vector3>()))
            .Returns(new MovementIntent(Vector3.Zero, true, false, false));

        var controller = new LocalPlayerController(entity, mockCamera.Object, world, mockInput.Object);

        mockPhysicsSystem.Setup(p => p.MoveAndResolve(It.IsAny<Vector3>(), It.IsAny<Vector3>(), It.IsAny<Vector3>()))
            .Returns((Vector3 pos, Vector3 move, Vector3 size) => pos + move);

        // Run one update
        controller.OnUpdate(0.016f);
        controller.OnFixedUpdate(0.016f);

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
        var mockInput = new Mock<IInputProvider>();
        mockInput.Setup(i => i.GetMovementIntent(It.IsAny<Vector3>(), It.IsAny<Vector3>()))
            .Returns(new MovementIntent(Vector3.Zero, true, false, false));

        var controller = new LocalPlayerController(entity, mockCamera.Object, world, mockInput.Object);

        mockPhysicsSystem.Setup(p => p.MoveAndResolve(It.IsAny<Vector3>(), It.IsAny<Vector3>(), It.IsAny<Vector3>()))
            .Returns((Vector3 pos, Vector3 move, Vector3 size) => pos + move);

        // Run updates until we reach peak height or timeout
        var maxObservedY = 62.5f;
        for (var i = 0; i < 100; i++)
        {
            controller.OnUpdate(0.016f);
            controller.OnFixedUpdate(0.016f);
            if (entity.Position.Y > maxObservedY) maxObservedY = entity.Position.Y;
        }

        // Peak Y should be above 64.0 (surface) to allow jumping out onto land
        maxObservedY.Should().BeGreaterThan(64.0f);
    }

    [Fact]
    public void Properties_WhenAccessedBeforeSense_ShouldNotThrow()
    {
        // Setup
        var world = new World();
        var mockCamera = new Mock<ICamera>();
        var mockPhysicsSystem = new Mock<IPhysicsSystem>();
        var entity = new PhysicsEntity(new Transform(), mockPhysicsSystem.Object);
        var controller = new LocalPlayerController(entity, mockCamera.Object, world,_inputProvider);

        // Access properties that use _sensor.LastSense
        var act = () =>
        {
            _ = controller.BlockBelow;
            _ = controller.BlockAbove;
            _ = controller.IsSwimming;
            _ = controller.IsUnderwater;
            _ = controller.IsOnWaterSurface;
            _ = controller.SubmersionDepth;
            _ = controller.IsGrounded;
            _ = controller.Yaw;
            _ = controller.Pitch;
            _ = controller.Roll;
            _ = controller.NormalizedYaw;
            _ = controller.Heading;
        };

        // Assert
        act.Should().NotThrow();
    }
}
