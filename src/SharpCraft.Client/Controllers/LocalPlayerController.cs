using System.Numerics;
using SharpCraft.Engine.Rendering.Cameras;
using SharpCraft.Sdk.Numerics;
using SharpCraft.Engine.Physics;
using SharpCraft.Engine.Physics.Motors;
using SharpCraft.Engine.Universe;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Input;
using SharpCraft.Sdk.Physics;
using SharpCraft.Engine.Physics.Sensors.Spatial;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.Client.Controllers;

public class LocalPlayerController(PhysicsEntity entity, ICamera camera, World world, IInputProvider inputProvider) : IPlayer
{
    private readonly GeospatialSensor _sensor = new();
    private readonly DefaultPlayerMotor _motor = new();

    public Transform Transform => entity.Transform;
    public string Name => nameof(LocalPlayerController);
    public string Id { get; } = Guid.NewGuid().ToString();
    public PhysicsEntity Entity => entity;
    IPhysicsEntity IPlayer.Entity => entity;
    public Block BlockBelow => _sensor.LastSense?.BlockBelow ?? default;
    public Block BlockAbove => _sensor.LastSense?.BlockAbove ?? default;
    public bool IsSwimming => _sensor.LastSense?.IsSwimming ?? false;
    public bool IsUnderwater => _sensor.LastSense?.IsUnderwater ?? false;
    public bool IsOnWaterSurface => _sensor.LastSense?.IsOnWaterSurface ?? false;
    public float SubmersionDepth => _sensor.LastSense?.SubmersionDepth ?? 0f;

    public bool IsFlying { get; set; }
    public bool UseDevSpeedBoost { get; set; } = true;

    public bool IsGrounded => _sensor.LastSense?.IsGrounded ?? false;
    public float Friction => _motor.Friction;

    /// <summary>
    /// Gets the current yaw angle in degrees (0 = North, 90 = East).
    /// </summary>
    public float Yaw => _yaw;

    /// <summary>
    /// Gets the current pitch angle in degrees.
    /// </summary>
    public float Pitch => camera is FirstPersonCamera fpc ? fpc.Pitch : (_sensor.LastSense?.Pitch ?? 0f);

    /// <summary>
    /// Gets the yaw angle normalized to [0, 360) degrees.
    /// </summary>
    public float NormalizedYaw => (_yaw % 360 + 360) % 360;

    /// <summary>
    /// Gets the compass heading based on the current yaw.
    /// </summary>
    public string Heading => MathUtils.GetHeading(_yaw);

    private float _yaw;
    private MovementIntent _pendingIntent;
    
    /// <summary>
    /// Gets the last movement intent processed by the controller.
    /// </summary>
    public MovementIntent LastIntent => _pendingIntent;

    public void OnUpdate(double deltaTime)
    {
        SensorPass();

        // Gather input and handle look immediately for responsiveness
        HandleLook(inputProvider.GetLookDelta());

        _pendingIntent = inputProvider.GetMovementIntent(camera.Forward, camera.Right);
        _pendingIntent = _pendingIntent with
        {
            IsFlying = IsFlying,
            UseDevSpeedBoost = UseDevSpeedBoost,
        };
    }

    public void OnFixedUpdate(double fixedDeltaTime)
    {
        var deltaTime = (float)fixedDeltaTime;

        _motor.SensorData = _sensor.LastSense;
        _motor.ApplyForces(entity, _pendingIntent, deltaTime);

        entity.Update(deltaTime);
    }

    private void SensorPass()
    {
        _sensor.Sense(world, entity);
        
        // Sync _yaw from entity rotation if it's the first time or if external change occurred
        // Rotation is applied as -_yaw, so Heading from sensor (rotation angle) is -_yaw
        var rotationHeading = _sensor.LastSense?.Heading ?? 0f;
        _yaw = -rotationHeading;
    }

    private void HandleLook(LookDelta lookDelta)
    {
        if (lookDelta == default) return;

        // Yaw: 0 = North (-Z), 90 = East (+X)
        // Mouse X-offset positive (moving right) should increase yaw (turning East)
        _yaw += lookDelta.Yaw;

        // Apply rotation. 
        // In a right-handed system, a positive rotation around Y moves +Z towards +X.
        // Since Forward is -Z, a positive rotation around Y moves -Z towards -X (West).
        // To make a positive yaw (turning East) work, we need a negative rotation around Y.
        entity.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -_yaw * MathF.PI / 180f);

        if (camera is FirstPersonCamera fpc)
        {
            fpc.HandleMouse(0, lookDelta.Pitch);
        }
    }
}