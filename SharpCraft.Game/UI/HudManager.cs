using SharpCraft.Core;
using SharpCraft.Game.Controllers;
using SharpCraft.Game.UI.Debug;
using SharpCraft.Game.UI.Main;
using SharpCraft.Game.UI.Settings;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace SharpCraft.Game.UI;

public class HudManager : IDisposable
{
    private readonly ImGuiController _controller;
    private readonly GL _gl;
    private readonly IWindow _window;
    private readonly IInputContext _input;
    private bool _disposed;
    private readonly Dictionary<string, IHud> _huds = [];

    public GraphicsSettingsHud? Settings => GetHud<GraphicsSettingsHud>();
    public DebugHud? Debug => GetHud<DebugHud>();

    public HudManager(GL gl, IWindow window, IInputContext input)
    {
        _gl = gl;
        _window = window;
        _input = input;
        _controller = new ImGuiController(gl, window, input);
        input.Keyboards[0].KeyUp += OnKeyUp;
    }

    public async Task InitializeAsync()
    {
        RegisterHud(new DebugHud());

        var mainHud = new MainHud(_window, _gl);
        await mainHud.LoadSteamAvatar();
        RegisterHud(mainHud);

        var graphicsSettingsHud = new GraphicsSettingsHud();
        graphicsSettingsHud.OnVisibilityChanged += UpdateCursorMode;
        RegisterHud(graphicsSettingsHud);
    }

    private void RegisterHud(Hud hud)
    {
        _huds[hud.Name] = hud;
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        if (key == Key.F3)
        {
            Settings?.IsVisible = !Settings.IsVisible;
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
        if (Settings?.IsVisible ?? false)
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

    private T? GetHud<T>() where T : class, IHud => _huds.Values
        .OfType<T>()
        .FirstOrDefault();

    public void Update(double deltaTime)
    {
        if (Settings != null)
        {
            if (_window.VSync != Settings.VSync)
            {
                _window.VSync = Settings.VSync;
                System.Console.WriteLine($"[DEBUG_LOG] VSync changed to: {_window.VSync}");
            }

            if (!Settings.VSync)
            {
                if (_window.FramesPerSecond != 0 || _window.UpdatesPerSecond != 0)
                {
                    _window.FramesPerSecond = 0;
                    _window.UpdatesPerSecond = 0;
                    System.Console.WriteLine("[DEBUG_LOG] FPS/UPS uncapped (set to 0)");
                }
            }
        }

        _controller.Update((float)deltaTime);
        foreach (var hud in _huds.Values)
        {
            hud.Update(deltaTime);
        }
    }

    public void Render(float deltaTime, World world, LocalPlayerController? player)
    {
        foreach (var hud in _huds)
        {
            hud.Value.Draw(deltaTime, world, player);
        }
        _controller.Render();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            var disposableHuds = _huds.Values.OfType<IDisposable>();
            foreach (var disposableHud in disposableHuds)
            {
                disposableHud.Dispose();
            }
            
            _controller.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}