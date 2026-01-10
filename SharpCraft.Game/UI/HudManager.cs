using SharpCraft.Core;
using SharpCraft.Game.Controllers;
using SharpCraft.Game.UI.Debug;
using SharpCraft.Game.UI.Settings;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace SharpCraft.Game.UI;

public class HudManager : IDisposable
{
    private readonly ImGuiController _controller;
    private readonly DebugHud _debugHud;
    private readonly GraphicsSettingsHud _graphicsSettingsHud;
    private readonly IInputContext _input;
    private bool _disposed;

    public GraphicsSettingsHud Settings => _graphicsSettingsHud;
    public DebugHud Debug => _debugHud;

    public HudManager(GL gl, IWindow window, IInputContext input)
    {
        _input = input;
        _controller = new ImGuiController(gl, window, input);
        _debugHud = new DebugHud();
        _graphicsSettingsHud = new GraphicsSettingsHud();
        _graphicsSettingsHud.OnVisibilityChanged += UpdateCursorMode;


        input.Keyboards[0].KeyUp += OnKeyUp;
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        if (key == Key.F3)
        {
            _graphicsSettingsHud.IsVisible = !_graphicsSettingsHud.IsVisible;
            UpdateCursorMode();
        }

        if (key == Key.AltLeft)
        {
            UpdateCursorMode();
        }
    }

    private void UpdateCursorMode()
    {
        var mouse = _input.Mice[0];

        // If menu is open, always show cursor. Otherwise toggle based on Raw mode.
        if (_graphicsSettingsHud.IsVisible)
        {
            mouse.Cursor.CursorMode = CursorMode.Normal;
        }
        else
        {
            mouse.Cursor.CursorMode = mouse.Cursor.CursorMode == CursorMode.Raw ? CursorMode.Normal : CursorMode.Raw;
        }
        OnCursorModeChanged?.Invoke();
    }

    // We'll need a way to tell the game to reset mouse delta,
    // usually via an event or by the game checking the manager state.
    public event Action? OnCursorModeChanged;

    public void Update(double deltaTime)
    {
        _controller.Update((float)deltaTime);
    }

    public void Render(float deltaTime, World world, LocalPlayerController? player)
    {
        _debugHud.Draw(deltaTime, world, player);
        _graphicsSettingsHud.Draw();
        _controller.Render();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _controller.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}