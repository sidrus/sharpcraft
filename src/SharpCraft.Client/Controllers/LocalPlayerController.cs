using System.Numerics;
using SharpCraft.Client.Rendering.Cameras;
using SharpCraft.Sdk.Numerics;
using SharpCraft.Engine.Physics;
using SharpCraft.Engine.Physics.Motors;
using SharpCraft.Engine.Universe;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Input;
using SharpCraft.Sdk.Physics;
using SharpCraft.Sdk.Physics.Sensors;
using SharpCraft.Sdk.Physics.Sensors.Spatial;
using SharpCraft.Sdk.Universe;
using IMotor = SharpCraft.Sdk.Physics.Motors.IMotor;

namespace SharpCraft.Client.Controllers;

public class LocalPlayerController(PhysicsEntity entity, ICamera camera, World world, IInputProvider inputProvider) : IController, IPlayer
{
    private readonly GeospatialSensor _sensor = new();
    private readonly DefaultPlayerMotor _motor = new();

    public Transform Transform => entity.Transform;
    public string Name => nameof(LocalPlayerController);
    public string Id { get; } = Guid.NewGuid().ToString();
    public IMotor Motor => _motor;
    public ISensor<SpatialSensorData> SpatialSensor => _sensor;
    public PhysicsEntity Entity => entity;
    IPhysicsEntity IPlayer.Entity => entity;
    public Block BlockBelow => _sensor.LastSense?.BlockBelow ?? default;
    public Block BlockAbove => _sensor.LastSense?.BlockAbove ?? default;
    public bool IsSwimming => _sensor.LastSense?.IsSwimming ?? false;
    public bool IsUnderwater => _sensor.LastSense?.IsUnderwater ?? false;
    public bool IsOnWaterSurface => _sensor.LastSense?.IsOnWaterSurface ?? false;
    public float SubmersionDepth => _sensor.LastSense?.SubmersionDepth ?? 0f;

    public bool IsFlying { get; set; }

    public bool IsGrounded => _sensor.LastSense?.IsGrounded ?? false;
    public float Friction => _motor.Friction;

    /// <summary>
    /// Gets the current yaw angle in degrees.
    /// </summary>
    public float Yaw => _sensor.LastSense?.Heading ?? 0f;

    /// <summary>
    /// Gets the current pitch angle in degrees.
    /// </summary>
    public float Pitch => camera is FirstPersonCamera fpc ? fpc.Pitch : (_sensor.LastSense?.Pitch ?? 0f);

    /// <summary>
    /// Gets the current roll angle in degrees.
    /// </summary>
    public float Roll => _sensor.LastSense?.Roll ?? 0f;

    /// <summary>
    /// Gets the yaw angle normalized to [0, 360) degrees.
    /// </summary>
    public float NormalizedYaw => ((_sensor.LastSense?.Heading ?? 0f) % 360 + 360) % 360;

    /// <summary>
    /// Gets the compass heading based on the current yaw.
    /// </summary>
    public string Heading => MathUtils.GetHeading(_sensor.LastSense?.Heading ?? 0f);

    private float _yaw;
    private MovementIntent _pendingIntent;

    private readonly List<object> _components = new();

    public void OnUpdate(double deltaTime)
    {
        SensorPass();

        // Gather input and handle look immediately for responsiveness
        HandleLook(inputProvider.GetLookDelta());

        _pendingIntent = inputProvider.GetMovementIntent(camera.Forward, camera.Right);
        _pendingIntent = _pendingIntent with { IsFlying = IsFlying };
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
    }

    private void HandleLook(LookDelta lookDelta)
    {
        if (lookDelta == default) return;

        _yaw += lookDelta.Yaw;

        if (camera is FirstPersonCamera fpc)
        {
            fpc.HandleMouse(0, lookDelta.Pitch);
            entity.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, _yaw * MathF.PI / 180f);
        }
        else
        {
            entity.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, _yaw * MathF.PI / 180f);
        }
    }

    public T? GetComponent<T>() where T : class
    {
        return _components.OfType<T>().FirstOrDefault();
    }

    public void AddComponent<T>(T component) where T : class
    {
        _components.Add(component);
    }
}