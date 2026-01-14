using System.Numerics;
using SharpCraft.Core;
using Silk.NET.Input;

namespace SharpCraft.Client.Input;

public class InputManager : ILifecycle, IDisposable
{
    private readonly IInputContext _input;

    private bool _disposed;

    public IKeyboard Keyboard { get; }

    public IMouse Mouse { get; }

    public Vector2 MouseDelta { get; private set; }

    private Vector2 _lastMousePos;

    public InputManager(IInputContext input)
    {
        _input = input;
        Keyboard = _input.Keyboards[0];
        Mouse = _input.Mice[0];

        Mouse.MouseMove += (_, pos) =>
        {
            MouseDelta = pos - _lastMousePos;
            _lastMousePos = pos;
        };
    }

    public void OnUpdate(double deltaTime)
    {
        // Reserved for future use if needed
    }

    public void PostUpdate() => MouseDelta = Vector2.Zero;

    public void OnDestroy()
    {
        Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _input.Dispose();
            }

            _disposed = true;
        }
    }
}