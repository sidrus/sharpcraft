using System.Numerics;
using SharpCraft.Sdk.Input;
using SharpCraft.Sdk.Physics;
using Silk.NET.Input;

namespace SharpCraft.Client.Input;

/// <summary>
/// Provides input for controlling an entity using keyboard and mouse.
/// </summary>
public class KeyboardMouseInputProvider : IInputProvider
{
    private readonly IInputContext _input;
    private Vector2 _lastMousePos;
    private bool _firstMouseMove = true;
    private const float Sensitivity = 0.1f;
    private LookDelta _lookDelta;
    private bool _isSprinting;

    public KeyboardMouseInputProvider(IInputContext input)
    {
        _input = input;
        
        foreach (var keyboard in _input.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
        }

        foreach (var mouse in _input.Mice)
        {
            mouse.MouseMove += OnMouseMove;
        }
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        if (key == Key.ControlLeft)
        {
            _isSprinting = !_isSprinting;
        }
    }

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        if (mouse.Cursor.CursorMode != CursorMode.Raw)
        {
            _firstMouseMove = true;
            return;
        }

        if (_firstMouseMove)
        {
            _lastMousePos = position;
            _firstMouseMove = false;
            return;
        }

        var deltaX = position.X - _lastMousePos.X;
        var deltaY = position.Y - _lastMousePos.Y;
        _lastMousePos = position;

        _lookDelta = new LookDelta(
            _lookDelta.Yaw - deltaX * Sensitivity,
            _lookDelta.Pitch - deltaY * Sensitivity
        );
    }

    /// <inheritdoc />
    public MovementIntent GetMovementIntent(Vector3 forward, Vector3 right)
    {
        var keyboard = _input.Keyboards[0];
        var moveDir = Vector3.Zero;

        if (keyboard.IsKeyPressed(Key.W)) moveDir += forward;
        if (keyboard.IsKeyPressed(Key.S)) moveDir -= forward;
        if (keyboard.IsKeyPressed(Key.A)) moveDir -= right;
        if (keyboard.IsKeyPressed(Key.D)) moveDir += right;

        if (moveDir.LengthSquared() > 0)
            moveDir = Vector3.Normalize(moveDir);

        return new MovementIntent(
            moveDir,
            keyboard.IsKeyPressed(Key.Space),
            keyboard.IsKeyPressed(Key.ShiftLeft),
            false, // Flying state should probably be managed by the controller or motor
            _isSprinting
        );
    }

    /// <inheritdoc />
    public LookDelta GetLookDelta()
    {
        var delta = _lookDelta;
        _lookDelta = default;
        return delta;
    }
}
