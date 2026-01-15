using System.Numerics;
using SharpCraft.Client.Controllers;
using SharpCraft.Client.Rendering.Cameras;
using SharpCraft.Sdk.Numerics;
using SharpCraft.Engine.Physics;
using SharpCraft.Engine.Universe;
using SharpCraft.Sdk.Input;
using SharpCraft.Sdk.Physics;
using Moq;
using AwesomeAssertions;
using Xunit;

namespace SharpCraft.Client.Tests.Controllers;

public class PitchClampingTests
{
    [Fact]
    public void Pitch_ShouldBeClamped_WhenLookDeltaIsLarge()
    {
        // Arrange
        var mockPhysicsSystem = new Mock<IPhysicsSystem>();
        var entity = new PhysicsEntity(new Transform(), mockPhysicsSystem.Object);
        var world = new World();
        var camera = new FirstPersonCamera(entity, Vector3.Zero);
        var mockInput = new Mock<IInputProvider>();
        
        var controller = new LocalPlayerController(entity, camera, world, mockInput.Object);
        
        // Act
        // Move mouse UP significantly (Pitch should increase)
        var largeUpDelta = new LookDelta(0, 100f);
        
        // We need to simulate the update loop or call HandleLook via reflection if private, 
        // but HandleLook is called in OnUpdate.
        mockInput.Setup(i => i.GetLookDelta()).Returns(largeUpDelta);
        
        controller.OnUpdate(0.016f);
        
        // Assert
        camera.Pitch.Should().BeInRange(-89f, 89f);
        controller.Pitch.Should().BeInRange(-89f, 89f);
    }
    
    [Fact]
    public void Pitch_ShouldBeClamped_WhenMultipleLookDeltasAreApplied()
    {
        // Arrange
        var mockPhysicsSystem = new Mock<IPhysicsSystem>();
        var entity = new PhysicsEntity(new Transform(), mockPhysicsSystem.Object);
        var world = new World();
        var camera = new FirstPersonCamera(entity, Vector3.Zero);
        var mockInput = new Mock<IInputProvider>();
        
        var controller = new LocalPlayerController(entity, camera, world, mockInput.Object);
        
        // Act
        mockInput.SetupSequence(i => i.GetLookDelta())
            .Returns(new LookDelta(0, 50f))
            .Returns(new LookDelta(0, 50f))
            .Returns(new LookDelta(0, 50f));
            
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
        var mockPhysicsSystem = new Mock<IPhysicsSystem>();
        var entity = new PhysicsEntity(new Transform(), mockPhysicsSystem.Object);
        var world = new World();
        var camera = new FirstPersonCamera(entity, Vector3.Zero);
        var mockInput = new Mock<IInputProvider>();
        var controller = new LocalPlayerController(entity, camera, world, mockInput.Object);

        // Act
        mockInput.Setup(i => i.GetLookDelta()).Returns(new LookDelta(0, 45f));
        controller.OnUpdate(0.016f);
        
        // At this point, camera.Pitch should be 45.
        // entity.Rotation should have pitch 45.
        
        var forward = camera.Forward;
        
        // If it's double-applying, forward.Y will be around sin(90) = 1.
        // If it's correct, forward.Y should be sin(45) approx 0.707.
        
        forward.Y.Should().BeApproximately(MathF.Sin(45f * MathF.PI / 180f), 0.01f);
    }
}
