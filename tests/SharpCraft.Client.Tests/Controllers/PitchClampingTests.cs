using AwesomeAssertions;
using NSubstitute;
using SharpCraft.Client.Controllers;
using SharpCraft.Engine.Physics;
using SharpCraft.Engine.Rendering.Cameras;
using SharpCraft.Engine.Universe;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Input;
using SharpCraft.Sdk.Physics;
using SharpCraft.Sdk.Universe;
using System.Numerics;

namespace SharpCraft.Client.Tests.Controllers;

public class PitchClampingTests
{
    [Fact]
    public void Pitch_ShouldBeClamped_WhenLookDeltaIsLarge()
    {
        // Arrange
        var mockPhysicsSystem = Substitute.For<IPhysicsSystem>();
        var entity = new PhysicsEntity(new Transform(), mockPhysicsSystem);
        var world = new World(Substitute.For<IWorldGenerator>(), 0, Substitute.For<IBlockRegistry>());
        var camera = new FirstPersonCamera(entity, Vector3.Zero);
        var mockInput = Substitute.For<IInputProvider>();

        var controller = new LocalPlayerController(entity, camera, world, mockInput, Substitute.For<IBlockRegistry>());

        // Act
        // Move mouse UP significantly (Pitch should increase)
        var largeUpDelta = new LookDelta(0, 100f);

        // We need to simulate the update loop or call HandleLook via reflection if private, 
        // but HandleLook is called in OnUpdate.
        mockInput.GetLookDelta().Returns(largeUpDelta);

        controller.OnUpdate(0.016f);

        // Assert
        camera.Pitch.Should().BeInRange(-89f, 89f);
        controller.Pitch.Should().BeInRange(-89f, 89f);
    }

    [Fact]
    public void Pitch_ShouldBeClamped_WhenMultipleLookDeltasAreApplied()
    {
        // Arrange
        var mockPhysicsSystem = Substitute.For<IPhysicsSystem>();
        var entity = new PhysicsEntity(new Transform(), mockPhysicsSystem);
        var world = new World(Substitute.For<IWorldGenerator>(), 0, Substitute.For<IBlockRegistry>());
        var camera = new FirstPersonCamera(entity, Vector3.Zero);
        var mockInput = Substitute.For<IInputProvider>();

        var controller = new LocalPlayerController(entity, camera, world, mockInput, Substitute.For<IBlockRegistry>());

        // Act
        mockInput.GetLookDelta().Returns(
            new LookDelta(0, 50f),
            new LookDelta(0, 50f),
            new LookDelta(0, 50f));

        controller.OnUpdate(0.016f); // Pitch = 50
        controller.OnUpdate(0.016f); // Pitch = 89 (clamped from 100)
        controller.OnUpdate(0.016f); // Pitch = 89 (clamped from 139)

        // Assert
        camera.Pitch.Should().Be(89f);
    }

    [Fact]
    public void Camera_Forward_ShouldMatchPitch()
    {
        // Arrange
        var mockPhysicsSystem = Substitute.For<IPhysicsSystem>();
        var entity = new PhysicsEntity(new Transform(), mockPhysicsSystem);
        var world = new World(Substitute.For<IWorldGenerator>(), 0, Substitute.For<IBlockRegistry>());
        var camera = new FirstPersonCamera(entity, Vector3.Zero);
        var mockInput = Substitute.For<IInputProvider>();
        var controller = new LocalPlayerController(entity, camera, world, mockInput, Substitute.For<IBlockRegistry>());

        // Act
        mockInput.GetLookDelta().Returns(new LookDelta(0, 45f));
        controller.OnUpdate(0.016f);

        // At this point, camera.Pitch should be 45.
        // entity.Rotation should have pitch 45.

        var forward = camera.Forward;

        // If it's double-applying, forward.Y will be around sin(90) = 1.
        // If it's correct, forward.Y should be sin(45) approx 0.707.

        forward.Y.Should().BeApproximately(MathF.Sin(45f * MathF.PI / 180f), 0.01f);
    }
}