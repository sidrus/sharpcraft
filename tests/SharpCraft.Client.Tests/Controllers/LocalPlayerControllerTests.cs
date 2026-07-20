using AwesomeAssertions;
using NSubstitute;
using SharpCraft.Client.Controllers;
using SharpCraft.Engine.Blocks;
using SharpCraft.Engine.Physics;
using SharpCraft.Engine.Rendering.Cameras;
using SharpCraft.Engine.Universe;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Input;
using SharpCraft.Sdk.Physics;
using SharpCraft.Sdk.Resources;
using SharpCraft.Sdk.Universe;
using System.Numerics;

namespace SharpCraft.Client.Tests.Controllers;

public class LocalPlayerControllerTests
{
    private readonly IInputProvider _inputProvider = Substitute.For<IInputProvider>();

    private static readonly IBlockRegistry Blocks = CreateBlocks();

    private static IBlockRegistry CreateBlocks()
    {
        var registry = new BlockRegistry();
        var water = new ResourceLocation("sharpcraft", "water");
        var stone = new ResourceLocation("sharpcraft", "stone");
        registry.Register(water, new BlockDefinition(water, "Water", Flags: BlockFlags.Transparent | BlockFlags.Fluid,
            Fluid: new FluidProperties(1000f, 0.15f, -2f, 2f, 4f, 0.8f, -2f, 0.5f)));
        registry.Register(stone, new BlockDefinition(stone, "Stone"));
        return registry;
    }

    [Fact]
    public void Update_WhenHoldingSpaceOnWaterSurface_ShouldEventuallySubmergeDeeply()
    {
        // Setup
        var world = new World(Substitute.For<IWorldGenerator>(), 0, Blocks);
        // Water at Y=63 (occupies 63.0 to 64.0)
        for (var x = -5; x <= 5; x++)
        {
            for (var z = -5; z <= 5; z++)
            {
                world.SetBlock(x, 63, z, "sharpcraft:water");
            }
        }

        var mockCamera = Substitute.For<ICamera>();
        var mockPhysicsSystem = Substitute.For<IPhysicsSystem>();

        // Entity starts at Y=64.0 (feet at water surface)
        var transform = new Transform { Position = new Vector3(0, 64.0f, 0) };
        var entity = new PhysicsEntity(transform, mockPhysicsSystem);
        var mockInput = Substitute.For<IInputProvider>();
        mockInput.GetMovementIntent(Arg.Any<Vector3>(), Arg.Any<Vector3>())
            .Returns(new MovementIntent(Vector3.Zero, true, false, false));

        var controller = new LocalPlayerController(entity, mockCamera, world, mockInput, Blocks);

        // Mock MoveAndResolve to just apply movement
        mockPhysicsSystem.MoveAndResolve(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector3>())
            .Returns(ci => ci.ArgAt<Vector3>(0) + ci.ArgAt<Vector3>(1));

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
        var world = new World(Substitute.For<IWorldGenerator>(), 0, Blocks);
        // Water at Y=63 (occupies 63.0 to 64.0)
        for (var x = -5; x <= 5; x++)
        {
            for (var z = -5; z <= 5; z++)
            {
                world.SetBlock(x, 63, z, "sharpcraft:water");
            }
        }

        var mockCamera = Substitute.For<ICamera>();
        var mockPhysicsSystem = Substitute.For<IPhysicsSystem>();

        // Entity starts at Y=63.0 (SubmersionDepth = 1.0m)
        // At this depth, IsSwimming is true, but SubmersionDepth <= 1.1f
        var transform = new Transform { Position = new Vector3(0, 63.0f, 0) };
        var entity = new PhysicsEntity(transform, mockPhysicsSystem);
        var mockInput = Substitute.For<IInputProvider>();
        mockInput.GetMovementIntent(Arg.Any<Vector3>(), Arg.Any<Vector3>())
            .Returns(new MovementIntent(Vector3.Zero, true, false, false));

        var controller = new LocalPlayerController(entity, mockCamera, world, mockInput, Blocks);

        mockPhysicsSystem.MoveAndResolve(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector3>())
            .Returns(ci => ci.ArgAt<Vector3>(0) + ci.ArgAt<Vector3>(1));

        // Run one update
        controller.OnUpdate(0.016f);
        controller.OnFixedUpdate(0.016f);

        // Should NOT have sunk (Y should be >= 63.0 or even > 63.0)
        // Currently, it will sink because no upward force is applied at 1.0m depth
        entity.Position.Y.Should().BeGreaterThanOrEqualTo(63.0f);
    }

    [Fact]
    public void Update_WhenHoldingSpaceInOpenWater_ShouldNotRiseAboveSurface()
    {
        // Setup: open water (Y=63 occupies 63.0 to 64.0), no land to climb onto.
        var world = new World(Substitute.For<IWorldGenerator>(), 0, Blocks);
        for (var x = -5; x <= 5; x++)
        {
            for (var z = -5; z <= 5; z++)
            {
                world.SetBlock(x, 63, z, "sharpcraft:water");
            }
        }

        var mockCamera = Substitute.For<ICamera>();
        var mockPhysicsSystem = Substitute.For<IPhysicsSystem>();

        // Entity starts at Y=62.5 (SubmersionDepth = 1.5m)
        var transform = new Transform { Position = new Vector3(0, 62.5f, 0) };
        var entity = new PhysicsEntity(transform, mockPhysicsSystem);
        var mockInput = Substitute.For<IInputProvider>();
        mockInput.GetMovementIntent(Arg.Any<Vector3>(), Arg.Any<Vector3>())
            .Returns(new MovementIntent(Vector3.Zero, true, false, false));

        var controller = new LocalPlayerController(entity, mockCamera, world, mockInput, Blocks);

        mockPhysicsSystem.MoveAndResolve(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector3>())
            .Returns(ci => ci.ArgAt<Vector3>(0) + ci.ArgAt<Vector3>(1));

        // Run updates, tracking the highest point reached while holding jump.
        var maxObservedY = 62.5f;
        for (var i = 0; i < 100; i++)
        {
            controller.OnUpdate(0.016f);
            controller.OnFixedUpdate(0.016f);
            if (entity.Position.Y > maxObservedY)
            {
                maxObservedY = entity.Position.Y;
            }
        }

        // The player should swim up toward the surface...
        maxObservedY.Should().BeGreaterThan(63.0f);
        // ...but must NOT rise above the waterline (no "walking on water" by holding jump).
        maxObservedY.Should().BeLessThan(64.0f);
    }

    [Fact]
    public void Update_WhenJumpingNextToLedge_ShouldClimbAboveSurface()
    {
        // Setup: water at Y=63, with a solid block at the waterline the player can climb onto.
        var world = new World(Substitute.For<IWorldGenerator>(), 0, Blocks);
        for (var x = -5; x <= 5; x++)
        {
            for (var z = -5; z <= 5; z++)
            {
                world.SetBlock(x, 63, z, "sharpcraft:water");
            }
        }

        // A solid ledge directly beside the player (foot level water block replaced with stone,
        // open air above it) — something to climb out onto.
        world.SetBlock(1, 63, 0, "sharpcraft:stone");

        var mockCamera = Substitute.For<ICamera>();
        var mockPhysicsSystem = Substitute.For<IPhysicsSystem>();

        // Entity starts at Y=62.5 (submerged) next to the ledge.
        var transform = new Transform { Position = new Vector3(0, 62.5f, 0) };
        var entity = new PhysicsEntity(transform, mockPhysicsSystem);
        var mockInput = Substitute.For<IInputProvider>();
        mockInput.GetMovementIntent(Arg.Any<Vector3>(), Arg.Any<Vector3>())
            .Returns(new MovementIntent(Vector3.Zero, true, false, false));

        var controller = new LocalPlayerController(entity, mockCamera, world, mockInput, Blocks);

        mockPhysicsSystem.MoveAndResolve(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector3>())
            .Returns(ci => ci.ArgAt<Vector3>(0) + ci.ArgAt<Vector3>(1));

        // Run updates until we reach peak height or timeout.
        var maxObservedY = 62.5f;
        for (var i = 0; i < 100; i++)
        {
            controller.OnUpdate(0.016f);
            controller.OnFixedUpdate(0.016f);
            if (entity.Position.Y > maxObservedY)
            {
                maxObservedY = entity.Position.Y;
            }
        }

        // With a ledge to climb onto, the jump should carry the player above the surface.
        maxObservedY.Should().BeGreaterThan(64.0f);
    }

    [Fact]
    public void Properties_WhenAccessedBeforeSense_ShouldNotThrow()
    {
        // Setup
        var world = new World(Substitute.For<IWorldGenerator>(), 0, Blocks);
        var mockCamera = Substitute.For<ICamera>();
        var mockPhysicsSystem = Substitute.For<IPhysicsSystem>();
        var entity = new PhysicsEntity(new Transform(), mockPhysicsSystem);
        var controller = new LocalPlayerController(entity, mockCamera, world, _inputProvider, Blocks);

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
            _ = controller.NormalizedYaw;
            _ = controller.Heading;
        };

        // Assert
        act.Should().NotThrow();
    }
}