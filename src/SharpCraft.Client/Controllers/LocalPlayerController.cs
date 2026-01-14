using System.Numerics;
using SharpCraft.Client.Rendering.Cameras;
using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Engine.World;
using SharpCraft.Engine.Blocks;
using SharpCraft.Sdk.Numerics;
using SharpCraft.Engine.Physics;
using SharpCraft.Sdk.Physics;
using Silk.NET.Input;

namespace SharpCraft.Client.Controllers;

public class LocalPlayerController(PhysicsEntity entity, ICamera camera, World world) : ILifecycle
{
    public PhysicsEntity Entity => entity;
    public const float WalkSpeed = 10f;
    public float Friction { get; private set; } = 0.05f;
    public Block BlockBelow { get; private set; }
    public Block BlockAbove { get; private set; }
    public bool IsSwimming { get; private set; }
    public bool IsUnderwater { get; private set; }
    public bool IsOnWaterSurface { get; private set; }
    public float SubmersionDepth { get; private set; }
    public bool IsFlying { get; set; }
    public bool IsGrounded => entity.IsGrounded;

    /// <summary>
    /// Gets the current yaw angle in degrees.
    /// </summary>
    public float Yaw => _yaw;

    /// <summary>
    /// Gets the current pitch angle in degrees.
    /// </summary>
    public float Pitch => camera is FirstPersonCamera fpc ? fpc.Pitch : 0;

    /// <summary>
    /// Gets the yaw angle normalized to [0, 360) degrees.
    /// </summary>
    public float NormalizedYaw => (_yaw % 360 + 360) % 360;

    /// <summary>
    /// Gets the compass heading based on the current yaw.
    /// </summary>
    public string Heading => MathUtils.GetHeading(_yaw);

    private Vector2 _lastMousePos;
    private bool _firstMouseMove = true;
    private const float Sensitivity = 0.1f;
    private float _yaw;

    private IKeyboard? _keyboard;

    public void OnFixedUpdate(double fixedDeltaTime)
    {
        Update((float)fixedDeltaTime, _keyboard);
    }

    public void Update(float deltaTime, IKeyboard? keyboard)
    {
        _keyboard = keyboard;
        if (keyboard is null) return;

        SenseSurroundings();

        var (gravity, terminalVelocity, walkSpeed, canJump) = CalculatePhysicsState();
        var moveDir = GetMovementDirection(keyboard);

        HandleJumpAndVerticalMovement(keyboard, deltaTime, walkSpeed, canJump);
        ApplyMovement(moveDir, walkSpeed, gravity, terminalVelocity, deltaTime);
    }

    private void SenseSurroundings()
    {
        var pos = entity.Position;
        var footY = (int)Math.Floor(pos.Y - 0.1f);

        // Small offset below feet
        BlockBelow = world.GetBlock(
            (int)Math.Floor(pos.X),
            footY,
            (int)Math.Floor(pos.Z));

        // Head level
        BlockAbove = world.GetBlock(
            (int)Math.Floor(pos.X),
            (int)Math.Floor(pos.Y + entity.Size.Y - 0.2f),
            (int)Math.Floor(pos.Z));

        var blockAtMid = world.GetBlock(
            (int)Math.Floor(pos.X),
            (int)Math.Floor(pos.Y + 0.5f),
            (int)Math.Floor(pos.Z));

        IsUnderwater = BlockAbove.Type == BlockType.Water;
        IsSwimming = IsUnderwater || blockAtMid.Type == BlockType.Water;
        IsOnWaterSurface = BlockBelow.Type == BlockType.Water && !IsSwimming;

        // Calculate SubmersionDepth (how deep the feet are into water)
        // Water surface is at floor(Y) + 1.0 if the block at floor(Y) is water.
        // If we are at Y=63.5 and Y=63 is water, depth = 64.0 - 63.5 = 0.5
        var currentBlockY = (int)Math.Floor(pos.Y);
        var blockAtFeet = world.GetBlock((int)Math.Floor(pos.X), currentBlockY, (int)Math.Floor(pos.Z));

        if (blockAtFeet.Type == BlockType.Water)
        {
            SubmersionDepth = (currentBlockY + 1) - pos.Y;
        }
        else if (world.GetBlock((int)Math.Floor(pos.X), currentBlockY - 1, (int)Math.Floor(pos.Z)).Type == BlockType.Water)
        {
            // If feet are just above water (e.g. at 64.04), depth is negative
            SubmersionDepth = currentBlockY - pos.Y;
        }
        else
        {
            SubmersionDepth = 0;
        }
    }

    private (float gravity, float terminalVelocity, float walkSpeed, bool canJump) CalculatePhysicsState()
    {
        var canJump = entity.IsGrounded && BlockBelow.IsSolid;

        var gravity = PhysicsConstants.DefaultGravity;
        var density = PhysicsConstants.AirDensity;
        var walkSpeed = WalkSpeed;
        var friction = canJump ? BlockBelow.Friction : PhysicsConstants.AirFriction;

        if (IsFlying)
        {
            gravity = 0f;
            density = IsSwimming || IsOnWaterSurface ? PhysicsConstants.WaterDensity : PhysicsConstants.AirDensity;
            walkSpeed = WalkSpeed * 2.5f;
            friction = PhysicsConstants.FlyingFriction;
        }
        else if (IsSwimming)
        {
            gravity = PhysicsConstants.WaterGravity;
            density = PhysicsConstants.WaterDensity;
            walkSpeed = WalkSpeed * 0.5f;
            friction = PhysicsConstants.WaterFriction;
        }
        else if (IsOnWaterSurface)
        {
            gravity = PhysicsConstants.DefaultGravity;
            density = PhysicsConstants.AirDensity;
            walkSpeed = WalkSpeed * 0.8f;
            friction = PhysicsConstants.WaterFriction;
        }

        Friction = friction;

        var terminalVelocity = IsFlying
            ? -100f
            : -PhysicsConstants.CalculateTerminalVelocity(
                PhysicsConstants.DefaultMass,
                gravity,
                density,
                PhysicsConstants.DefaultDragCoefficient,
                PhysicsConstants.DefaultCrossSectionalArea);

        return (gravity, terminalVelocity, walkSpeed, canJump);
    }

    private Vector3 GetMovementDirection(IKeyboard keyboard)
    {
        var moveDir = Vector3.Zero;
        if (keyboard.IsKeyPressed(Key.W)) moveDir += entity.Forward;
        if (keyboard.IsKeyPressed(Key.S)) moveDir -= entity.Forward;
        if (keyboard.IsKeyPressed(Key.A)) moveDir -= entity.Right;
        if (keyboard.IsKeyPressed(Key.D)) moveDir += entity.Right;

        if (!IsFlying)
            moveDir.Y = 0;

        if (moveDir.LengthSquared() > 0)
            moveDir = Vector3.Normalize(moveDir);

        return moveDir;
    }

    private void HandleJumpAndVerticalMovement(IKeyboard keyboard, float deltaTime, float walkSpeed, bool canJump)
    {
        if (IsFlying)
        {
            if (keyboard.IsKeyPressed(Key.Space))
                entity.Velocity.Y = MathUtils.Lerp(entity.Velocity.Y, walkSpeed, deltaTime * 10f);
            else if (keyboard.IsKeyPressed(Key.ShiftLeft))
                entity.Velocity.Y = MathUtils.Lerp(entity.Velocity.Y, -walkSpeed, deltaTime * 10f);
            else
                entity.Velocity.Y = MathUtils.Lerp(entity.Velocity.Y, 0, deltaTime * 10f);
        }
        else if (keyboard.IsKeyPressed(Key.Space))
        {
            if (IsSwimming)
            {
                // Provide a baseline upward movement when swimming
                entity.Velocity.Y = Math.Max(entity.Velocity.Y, 2.0f);

                // If deep enough, allow a stronger "kick" (jump)
                // This allows gaining momentum to potentially leave the water
                if (SubmersionDepth > 0.8f)
                {
                    entity.Velocity.Y = Math.Max(entity.Velocity.Y, 4.0f);
                }
            }
            else if (canJump)
            {
                entity.Velocity.Y = 5.0f; // Normal Jump
            }
        }
    }

    private void ApplyMovement(Vector3 moveDir, float walkSpeed, float gravity, float terminalVelocity, float deltaTime)
    {
        // Dolphin-launch prevention: apply extra drag at the surface
        if (IsSwimming && entity.Velocity.Y > 3.5f && !IsUnderwater)
        {
            entity.Velocity.Y = MathUtils.Lerp(entity.Velocity.Y, 3.5f, deltaTime * 10.0f);
        }

        // friction application
        var deltaFriction = 1.0f - MathF.Pow(1.0f - Friction, deltaTime * 60.0f);
        entity.Velocity.X = MathUtils.Lerp(entity.Velocity.X, moveDir.X * walkSpeed, deltaFriction);
        entity.Velocity.Z = MathUtils.Lerp(entity.Velocity.Z, moveDir.Z * walkSpeed, deltaFriction);

        // Apply gravity and clamp to terminal velocity
        entity.Velocity.Y += gravity * deltaTime;
        if (entity.Velocity.Y < terminalVelocity)
            entity.Velocity.Y = terminalVelocity;

        entity.Update(deltaTime);
    }

    public void ResetMouse()
    {
        _firstMouseMove = true;
    }

    public void HandleMouse(IMouse mouse, Vector2 position)
    {
        if (_firstMouseMove)
        {
            _lastMousePos = position;
            _firstMouseMove = false;
            return;
        }

        var lookX = position.X - _lastMousePos.X;
        var lookY = position.Y - _lastMousePos.Y;
        _lastMousePos = position;

        _yaw -= lookX * Sensitivity;
        entity.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, _yaw * MathF.PI / 180f);
        if (camera is FirstPersonCamera fpc)
        {
            fpc.HandleMouse(0, -lookY * Sensitivity);
        }
    }
}