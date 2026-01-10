using System.Numerics;
using SharpCraft.Core;
using SharpCraft.Core.Blocks;
using SharpCraft.Core.Numerics;
using SharpCraft.Core.Physics;
using SharpCraft.Game.Rendering;
using SharpCraft.Game.Rendering.Cameras;
using Silk.NET.Input;

namespace SharpCraft.Game.Controllers;

public class LocalPlayerController(PhysicsEntity entity, ICamera camera, World world)
{
    public PhysicsEntity Entity => entity;
    public const float WalkSpeed = 10f;
    public float Friction { get; private set; } = 0.05f;
    public Block BlockBelow { get; private set; }
    public Block BlockAbove { get; private set; }
    public bool IsSwimming { get; private set; }
    public bool IsUnderwater { get; private set; }

    private Vector2 _lastMousePos;
    private bool _firstMouseMove = true;
    private const float Sensitivity = 0.1f;
    private float _yaw;

    public void Update(double deltaTime, IKeyboard? keyboard)
    {
        if (keyboard is null) return;

        var dt = (float)deltaTime;
        var pos = entity.Position;

        // 1. SENSE: Detect surroundings
        BlockBelow = world.GetBlock(
            (int)Math.Floor(pos.X),
            (int)Math.Floor(pos.Y - 0.1f), // Small offset below feet
            (int)Math.Floor(pos.Z));

        BlockAbove = world.GetBlock(
            (int)Math.Floor(pos.X),
            (int)Math.Floor(pos.Y + entity.Size.Y - 0.2f), // Head level
            (int)Math.Floor(pos.Z));

        IsUnderwater = BlockAbove.Type == BlockType.Water;
        IsSwimming = BlockBelow.Type == BlockType.Water || IsUnderwater;

        // 2. DECIDE: Determine physics constants based on state
        var gravity = -9.81f;
        var terminalVelocity = -50f;
        var currentWalkSpeed = WalkSpeed;

        var canJump = entity.IsGrounded && BlockBelow.IsSolid;
        Friction = canJump ? BlockBelow.Friction : 0.05f;

        if (IsSwimming)
        {
            gravity = -2.0f;          // Buoyancy
            terminalVelocity = -2.0f; // Sinking cap
            currentWalkSpeed *= 0.5f; // Water resistance
            Friction = 0.15f;         // Fluid drag
        }

        // 3. INPUT: Process movement and vertical forces
        var moveDir = Vector3.Zero;
        if (keyboard.IsKeyPressed(Key.W)) moveDir += entity.Forward;
        if (keyboard.IsKeyPressed(Key.S)) moveDir -= entity.Forward;
        if (keyboard.IsKeyPressed(Key.A)) moveDir -= entity.Right;
        if (keyboard.IsKeyPressed(Key.D)) moveDir += entity.Right;
        moveDir.Y = 0;

        if (moveDir.LengthSquared() > 0) moveDir = Vector3.Normalize(moveDir);

        if (keyboard.IsKeyPressed(Key.Space))
        {
            if (IsSwimming)
            {
                // Swim up: Max prevents stacking with existing upward momentum
                entity.Velocity.Y = Math.Max(entity.Velocity.Y, 3.0f);
            }
            else if (canJump)
            {
                entity.Velocity.Y = 5.0f; // Normal Jump
            }
        }

        // 4. ACT: Apply calculated forces
        // Dolphin-launch prevention: apply extra drag at the surface
        if (IsSwimming && entity.Velocity.Y > 1.5f && !IsUnderwater)
        {
            entity.Velocity.Y = MathUtils.Lerp(entity.Velocity.Y, 1.5f, dt * 10.0f);
        }

        // friction application
        var deltaFriction = 1.0f - MathF.Pow(1.0f - Friction, dt * 60.0f);
        entity.Velocity.X = MathUtils.Lerp(entity.Velocity.X, moveDir.X * currentWalkSpeed, deltaFriction);
        entity.Velocity.Z = MathUtils.Lerp(entity.Velocity.Z, moveDir.Z * currentWalkSpeed, deltaFriction);

        // Apply gravity and clamp to terminal velocity
        entity.Velocity.Y += gravity * dt;
        if (entity.Velocity.Y < terminalVelocity) entity.Velocity.Y = terminalVelocity;

        entity.Update(dt);
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