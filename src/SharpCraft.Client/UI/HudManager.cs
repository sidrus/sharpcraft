using Microsoft.Extensions.Logging;
using SharpCraft.Client.Controllers;
using SharpCraft.Client.Rendering;
using SharpCraft.Client.Rendering.Lighting;
using SharpCraft.Client.UI.Chat;
using SharpCraft.Engine.Universe;
using SharpCraft.Engine.UI;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Diagnostics;
using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Sdk.UI;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace SharpCraft.Client.UI;

public partial class HudManager : ILifecycle, IDisposable, IHudRegistry
{
    private ImGuiController _controller;
    private readonly GL _gl;
    private readonly IWindow _window;
    private readonly IInputContext _input;
    private bool _disposed;
    private readonly Dictionary<string, IHud> _huds = [];
    private readonly IGui _gui;
    private readonly IGraphicsSettings _fallbackSettings = new DefaultGraphicsSettings();

    public IGraphicsSettings Settings => GetHud<IGraphicsSettings>() ?? _fallbackSettings;
    public ChatHud? Chat => GetHud<ChatHud>();

    public HudManager(GL gl, IWindow window, IInputContext input, ILogger<HudManager> logger)
    {
        _gl = gl;
        _window = window;
        _input = input;
        _controller = new ImGuiController(gl, window, input);
        _gui = new ImGuiGui();
        input.Keyboards[0].KeyUp += OnKeyUp;
    }

    public void OnAwake()
    {
        _controller = new ImGuiController(_gl, _window, _input);
    }

    public async Task InitializeAsync()
    {
        // These are client-specific HUDs that stay in the client for now
        var chatHud = new ChatHud();
        chatHud.OnVisibilityChanged += UpdateCursorMode;
        RegisterHud(chatHud);
        
        UpdateCursorMode();

        await Task.CompletedTask;
    }

    public void RegisterHud(string name, Action<double> drawAction)
    {
        RegisterHud(new SdkHudWrapper(name, drawAction));
    }

    public void RegisterHud(IHud hud)
    {
        _huds[hud.Name] = hud;
        
        if (hud is IInteractiveHud interactiveHud)
        {
            interactiveHud.OnVisibilityChanged += UpdateCursorMode;
        }
    }

    private sealed class SdkHudWrapper(string name, Action<double> drawAction) : IHud
    {
        public string Name { get; } = name;

        public void Draw(double deltaTime, IGui gui, IHudContext context)
        {
            drawAction(deltaTime);
        }

        public void OnAwake() { }
        public void OnUpdate(double deltaTime) { }
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        switch (key)
        {
            case Key.F3:
                Settings.IsVisible = !Settings.IsVisible;
                break;
            case Key.F4:
                var devHud = _huds.Values.OfType<IInteractiveHud>().FirstOrDefault(h => h.Name == "DeveloperHud");
                if (devHud != null) devHud.IsVisible = !devHud.IsVisible;
                break;
        }

        if (Chat is { IsTyping: false })
        {
            switch (key)
            {
                case Key.Enter:
                    Chat.StartTyping();
                    break;
                case Key.Slash:
                    Chat.StartTyping("/");
                    break;
            }
        }

        if (key == Key.AltLeft)
        {
            ToggleCursorMode();
        }
    }

    private void UpdateCursorMode()
    {
        var mouse = _input.Mice[0];

        var isAnyMenuVisible = _huds.Values
            .OfType<IInteractiveHud>()
            .Any(h => h.IsVisible);
        
        if (isAnyMenuVisible)
        {
            mouse.Cursor.CursorMode = CursorMode.Normal;
        }
        else
        {
            mouse.Cursor.CursorMode = CursorMode.Raw;
        }
        OnCursorModeChanged?.Invoke();
    }

    private void ToggleCursorMode()
    {
        var mouse = _input.Mice[0];
        
        var isAnyMenuVisible = _huds.Values
            .OfType<IInteractiveHud>()
            .Any(h => h.IsVisible);
        
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

    public event Action? OnCursorModeChanged;

    private T? GetHud<T>() where T : class => _huds.Values
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
    private ISharpCraftSdk? _sdk;
    private IEnumerable<IMod>? _mods;
    private IAvatarProvider? _avatar;
    private IDiagnosticsProvider? _diagnostics;

    public void SetContext(World world, LocalPlayerController? player, ChunkMeshManager? meshManager, LightingSystem? lighting, ISharpCraftSdk? sdk = null, IEnumerable<IMod>? mods = null, IAvatarProvider? avatar = null, IDiagnosticsProvider? diagnostics = null)
    {
        _world = world;
        _player = player;
        _meshManager = meshManager;
        _lighting = lighting;
        _sdk = sdk;
        _mods = mods;
        _avatar = avatar;
        _diagnostics = diagnostics;
    }

    public void OnRender(double deltaTime)
    {
        if (_world == null || _sdk == null || _mods == null) return;

        var context = new HudContext(_world, _player, _meshManager, _lighting, _sdk, _mods, _avatar, _diagnostics);
        foreach (var hud in _huds.Values)
        {
            hud.Draw(deltaTime, _gui, context);
        }
        _controller.Render();
    }

    public void Render(float deltaTime, World world, LocalPlayerController? player, ChunkMeshManager? meshManager, LightingSystem? lighting, ISharpCraftSdk? sdk = null, IEnumerable<IMod>? mods = null, IAvatarProvider? avatar = null, IDiagnosticsProvider? diagnostics = null)
    {
        SetContext(world, player, meshManager, lighting, sdk, mods, avatar, diagnostics);
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

    private sealed class DefaultGraphicsSettings : IGraphicsSettings
    {
        public string Name => "DefaultGraphicsSettings";
        public void Draw(double deltaTime, IGui gui, IHudContext context) { }
        public void OnAwake() { }
        public void OnUpdate(double deltaTime) { }
        
        public bool IsVisible { get; set; }
        public event Action? OnVisibilityChanged;
        public bool VSync { get; set; }
        public float Gamma { get; set; } = 1.6f;
        public float Exposure { get; set; } = 1.0f;
        public bool UseNormalMap { get; set; } = true;
        public float NormalStrength { get; set; } = 0.5f;
        public bool UseAoMap { get; set; } = true;
        public float AoMapStrength { get; set; } = 0.5f;
        public bool UseMetallicMap { get; set; } = true;
        public float MetallicStrength { get; set; } = 1.0f;
        public bool UseRoughnessMap { get; set; } = true;
        public float RoughnessStrength { get; set; } = 1.0f;
        public bool UseIBL { get; set; } = false;
        public float FogNearFactor { get; set; } = 0.3f;
        public float FogFarFactor { get; set; } = 0.95f;
        public int RenderDistance { get; set; } = 8;
    }
}