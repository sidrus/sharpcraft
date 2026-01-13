using Microsoft.Extensions.Logging;
using SharpCraft.Core;
using SharpCraft.Game.Controllers;
using SharpCraft.Game.Rendering;
using SharpCraft.Game.Rendering.Lighting;
using SharpCraft.Game.UI.Debug;
using SharpCraft.Game.UI.Main;
using SharpCraft.Game.UI.Settings;
using SharpCraft.Game.UI.Chat;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace SharpCraft.Game.UI;

public partial class HudManager : ILifecycle, IDisposable
{
    private ImGuiController _controller;
    private readonly GL _gl;
    private readonly IWindow _window;
    private readonly IInputContext _input;
    private bool _disposed;
    private readonly Dictionary<string, IHud> _huds = [];

    public GraphicsSettingsHud? Settings => GetHud<GraphicsSettingsHud>();
    public DebugHud? Debug => GetHud<DebugHud>();
    public DeveloperHud? Developer => GetHud<DeveloperHud>();
    public ChatHud? Chat => GetHud<ChatHud>();

    public HudManager(GL gl, IWindow window, IInputContext input, ILogger<HudManager> logger)
    {
        _gl = gl;
        _window = window;
        _input = input;
        _controller = new ImGuiController(gl, window, input);
        input.Keyboards[0].KeyUp += OnKeyUp;

        if (Settings != null)
        {
            Settings.OnVisibilityChanged += UpdateCursorMode;
        }
    }

    public void OnAwake()
    {
        _controller = new ImGuiController(_gl, _window, _input);
    }

    public async Task InitializeAsync()
    {
        RegisterHud(new DebugHud());

        var chatHud = new ChatHud();
        chatHud.OnVisibilityChanged += UpdateCursorMode;
        RegisterHud(chatHud);

        var mainHud = new MainHud(_window, _gl);
        await mainHud.LoadSteamAvatar();
        RegisterHud(mainHud);

        var graphicsSettingsHud = new GraphicsSettingsHud();
        graphicsSettingsHud.OnVisibilityChanged += UpdateCursorMode;
        RegisterHud(graphicsSettingsHud);

        var developerHud = new DeveloperHud();
        developerHud.OnVisibilityChanged += UpdateCursorMode;
        RegisterHud(developerHud);
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

        if (key == Key.F4 && Developer != null)
        {
            Developer.IsVisible = !Developer.IsVisible;
            UpdateCursorMode();
        }

        if (Chat is { IsTyping: false })
        {
            switch (key)
            {
                case Key.Enter:
                    Chat.StartTyping();
                    UpdateCursorMode();
                    break;
                case Key.Slash:
                    Chat.StartTyping("/");
                    UpdateCursorMode();
                    break;
            }
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
        var isAnyMenuVisible = (Settings?.IsVisible ?? false) || 
                             (Developer?.IsVisible ?? false) ||
                             (Chat?.IsTyping ?? false);
        
        if (isAnyMenuVisible)
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

    public void OnUpdate(double deltaTime)
    {
        if (Settings != null)
        {
            _window.VSync = Settings.VSync;
            if (!Settings.VSync)
            {
                _window.FramesPerSecond = 0;
                _window.UpdatesPerSecond = 0;
            }
        }

        _controller.Update((float)deltaTime);
        foreach (var hud in _huds.Values)
        {
            hud.OnUpdate(deltaTime);
        }
    }

    private World? _world;
    private LocalPlayerController? _player;
    private ChunkMeshManager? _meshManager;
    private LightingSystem? _lighting;

    public void SetContext(World world, LocalPlayerController? player, ChunkMeshManager? meshManager, LightingSystem? lighting)
    {
        _world = world;
        _player = player;
        _meshManager = meshManager;
        _lighting = lighting;
    }

    public void OnRender(double deltaTime)
    {
        if (_world == null) return;

        var context = new HudContext(_world, _player, _meshManager, _lighting);
        foreach (var hud in _huds)
        {
            hud.Value.Draw(deltaTime, context);
        }
        _controller.Render();
    }

    public void Render(float deltaTime, World world, LocalPlayerController? player, ChunkMeshManager? meshManager, LightingSystem? lighting)
    {
        SetContext(world, player, meshManager, lighting);
        OnRender(deltaTime);
    }

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
                var disposableHuds = _huds.Values.OfType<IDisposable>();
                foreach (var disposableHud in disposableHuds)
                {
                    disposableHud.Dispose();
                }

                _controller.Dispose();
            }

            _disposed = true;
        }
    }
}