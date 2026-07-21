using SharpCraft.Client.Controllers;
using SharpCraft.Client.Input;
using SharpCraft.Client.UI.Chat;
using SharpCraft.Engine.Rendering.Chunks;
using SharpCraft.Engine.Rendering.Lighting;
using SharpCraft.Engine.Rendering.PostProcessing;
using SharpCraft.Engine.UI;
using SharpCraft.Engine.Universe;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Diagnostics;
using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Sdk.UI;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace SharpCraft.Client.UI;

public class HudManager : ILifecycle, IDisposable
{
    private readonly ImGuiController _controller;
    private readonly IWindow _window;
    private readonly IInputContext _input;
    private bool _disposed;
    private readonly HudRegistry _registry;
    private readonly IGui _gui;
    private readonly Keymap _keymap = new();

    private IReadOnlyList<IHud> Huds => _registry.RegisteredHuds;

    public IGraphicsSettings Settings
    {
        get;
    }

    public ChatHud? Chat => GetHud<ChatHud>();

    public HudManager(GL gl, IWindow window, IInputContext input, HudRegistry registry, IGraphicsSettings settings)
    {
        _window = window;
        _input = input;
        _registry = registry;
        Settings = settings;
        _controller = new ImGuiController(gl, window, input);
        _gui = new ImGuiGui();
        input.Keyboards[0].KeyUp += OnKeyUp;
    }

    public void OnAwake()
    {
    }

    public void Initialize()
    {
        // Client-specific HUDs register into the same store the mods used.
        _registry.RegisterHud(new ChatHud());
        _registry.RegisterHud(new AtmosphereControlHud());

        _keymap.Bind(Key.F3, () => ToggleInteractiveHud("GraphicsSettingsHud"));
        _keymap.Bind(Key.F4, () => ToggleInteractiveHud("DeveloperHud"));
        _keymap.Bind(Key.F5, () => ToggleInteractiveHud("AtmosphereControl"));
        _keymap.Bind(Key.AltLeft, ToggleCursorMode);

        // Any HUD that can open a menu drives the cursor mode.
        foreach (var interactiveHud in Huds.OfType<IInteractiveHud>())
        {
            interactiveHud.OnVisibilityChanged += UpdateCursorMode;
        }

        UpdateCursorMode();
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        _keymap.Handle(key);

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
    }

    private void UpdateCursorMode()
    {
        var mouse = _input.Mice[0];

        var isAnyMenuVisible = Huds
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

        var isAnyMenuVisible = Huds
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

    private T? GetHud<T>() where T : class => Huds
        .OfType<T>()
        .FirstOrDefault();

    private void ToggleInteractiveHud(string name)
    {
        var hud = Huds.OfType<IInteractiveHud>().FirstOrDefault(h => h.Name == name);
        if (hud != null)
        {
            hud.IsVisible = !hud.IsVisible;
        }
    }

    public void OnUpdate(double deltaTime)
    {
        _window.VSync = Settings.VSync;
        if (!Settings.VSync)
        {
            _window.FramesPerSecond = 0;
            _window.UpdatesPerSecond = 0;
        }

        foreach (var hud in Huds)
        {
            hud.OnUpdate(deltaTime);
        }
    }

    private World? _world;
    private LocalPlayerController? _player;
    private ChunkMeshManager? _meshManager;
    private LightingSystem? _lighting;
    private PostProcessingRenderer? _postProcessing;
    private ISharpCraftSdk? _sdk;
    private IEnumerable<IMod>? _mods;
    private IAvatarProvider? _avatar;
    private IDiagnosticsProvider? _diagnostics;

    public void SetContext(World world, LocalPlayerController? player, ChunkMeshManager? meshManager, LightingSystem? lighting, PostProcessingRenderer? postProcessing, ISharpCraftSdk? sdk = null, IEnumerable<IMod>? mods = null, IAvatarProvider? avatar = null, IDiagnosticsProvider? diagnostics = null)
    {
        _world = world;
        _player = player;
        _meshManager = meshManager;
        _lighting = lighting;
        _postProcessing = postProcessing;
        _sdk = sdk;
        _mods = mods;
        _avatar = avatar;
        _diagnostics = diagnostics;
    }

    public void OnRender(double deltaTime)
    {
        if (_world == null || _sdk == null || _mods == null)
        {
            return;
        }

        _controller.Update((float)deltaTime);

        var context = new HudContext(_player, _meshManager, _lighting, _postProcessing, _sdk, _mods, _avatar, _diagnostics);
        foreach (var hud in Huds)
        {
            hud.Draw(deltaTime, _gui, context);
        }
        _controller.Render();
    }

    public void Render(float deltaTime, World world, LocalPlayerController? player, ChunkMeshManager? meshManager, LightingSystem? lighting, PostProcessingRenderer? postProcessing, ISharpCraftSdk? sdk = null, IEnumerable<IMod>? mods = null, IAvatarProvider? avatar = null, IDiagnosticsProvider? diagnostics = null)
    {
        SetContext(world, player, meshManager, lighting, postProcessing, sdk, mods, avatar, diagnostics);
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
                var disposableHuds = Huds.OfType<IDisposable>();
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