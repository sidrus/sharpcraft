using Microsoft.Extensions.Logging;
using SharpCraft.Client.Controllers;
using SharpCraft.Client.Input;
using SharpCraft.Client.Integrations.Steam;
using SharpCraft.Client.UI;
using SharpCraft.Engine.Diagnostics;
using SharpCraft.Engine.Lifecycle;
using SharpCraft.Engine.Physics;
using SharpCraft.Engine.Rendering;
using SharpCraft.Engine.Rendering.Cameras;
using SharpCraft.Engine.Rendering.Lighting;
using SharpCraft.Engine.Rendering.Shaders;
using SharpCraft.Engine.Rendering.Textures;
using SharpCraft.Engine.UI;
using SharpCraft.Engine.Universe;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Sdk.Numerics;
using SharpCraft.Sdk.Physics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Steamworks;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SharpCraft.Client;

public partial class Game : IDisposable
{
    private readonly World _world;
    private readonly IWindow? _window;
    private GL? _gl;

    private readonly ILogger<Game> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISharpCraftSdk _sdk;
    private readonly IEnumerable<IMod> _mods;
    private InputManager? _input;
    private KeyboardMouseInputProvider? _inputProvider;
    private HudManager? _hudManager;
    private AvatarLoader? _avatarLoader;
    private DiagnosticsManager? _diagnosticsManager;
    private readonly LightingSystem _lightSystem;
    private readonly LifecycleManager _lifecycleManager;
    private WorldTime? _worldTime;
    private Sun? _sun;
    private DefaultRenderPipeline? _renderPipeline;
    private PostProcessingRenderer? _postProcessingRenderer;
    private ShaderProgram? _mainShader;
    private ICamera? _camera;
    private LocalPlayerController? _playerController;
    private TorchRenderer? _torchRenderer;
    private TextureAtlas? _atlas;
    private bool _useNormalMap = true;
    private bool _useAoMap = true;
    private bool _useMetallicMap = true;
    private bool _useRoughnessMap = true;

    private const double FixedDeltaTime = 1.0 / 60.0;
    private double _accumulator;
    private Vector2<int>? _lastPlayerChunk;
    private Task? _worldUpdateTask;

    public Game(IWindow window, World world, ILoggerFactory loggerFactory, ISharpCraftSdk sdk, IEnumerable<IMod> mods)
    {
        _world = world;
        _loggerFactory = loggerFactory;
        _sdk = sdk;
        _mods = mods;
        _logger = loggerFactory.CreateLogger<Game>();
        _lightSystem = (LightingSystem)sdk.Lighting;
        _lifecycleManager = new LifecycleManager();

        _window = window;
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Resize += OnResize;
        _window.Closing += Dispose;
    }

    private void OnResize(Vector2D<int> size)
    {
        _gl?.Viewport(0, 0, (uint)size.X, (uint)size.Y);
    }

    private void OnLoad()
    {
        try
        {
            LogCreatingOpenglContext();
            _gl = _window.CreateOpenGL();
            LogOpenglContextCreated();

            // Reversed-Z foundation (research §1, §12.2): flip the NDC depth range to [0,1] and
            // keep GL's native bottom-left origin. The reversed-Z projection + GL_GREATER depth
            // test + 0.0 depth clear are set up in InitializeGraphicsState / the render pipeline.
            _gl.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.ZeroToOne);

#if DEBUG
            // Synchronous KHR_debug output routed through the logger (research §12.5).
            InitializeGlDebugOutput();
#endif

            if (SteamClient.IsValid)
            {
                SteamFriends.SetRichPresence("status", "Exploring SharpCraft");
            }

            InitializeGraphicsState();
            InitializeSystems();
            RegisterInputHandlers();
        }
        catch (Exception e)
        {
            LogGameLoadFailed(e);
            Exit();
        }
    }

    private void OnUpdate(double deltaTime)
    {
        SteamClient.RunCallbacks();

        _lifecycleManager.Update(deltaTime);

        if (_input?.Mouse.Cursor.CursorMode == CursorMode.Raw)
        {
            _playerController?.OnUpdate(deltaTime);
        }

        _accumulator += deltaTime;

        while (_accumulator >= FixedDeltaTime)
        {
            _lifecycleManager.FixedUpdate(FixedDeltaTime);
            _playerController?.OnFixedUpdate(FixedDeltaTime);

            _accumulator -= FixedDeltaTime;
        }

        UpdateWorldChunks();

        if (_diagnosticsManager != null && _renderPipeline != null)
        {
            var loadedChunks = _world.GetLoadedChunks().Count();
            var meshQueue = _renderPipeline.MeshManager.DirtyChunksCount + _renderPipeline.MeshManager.ProcessingChunksCount;
            var activeLights = _lightSystem.GetActivePointLights().Count() + 1;
            var velocity = _playerController?.Entity.Velocity.Length() ?? 0f;

            _diagnosticsManager.Update(deltaTime, loadedChunks, meshQueue, activeLights, velocity, _worldTime?.FormattedTime ?? string.Empty);
        }

        _hudManager?.OnUpdate(deltaTime);
        _input?.PostUpdate();
    }

    private void UpdateWorldChunks()
    {
        if (_playerController == null || _hudManager?.Settings == null)
        {
            return;
        }

        var playerPos = _playerController.Entity.Position;
        var currentChunkX = (int)Math.Floor(playerPos.X / World.ChunkSize);
        var currentChunkZ = (int)Math.Floor(playerPos.Z / World.ChunkSize);
        var currentChunk = new Vector2<int>(currentChunkX, currentChunkZ);

        var canStartNew = _worldUpdateTask == null ||
            (_worldUpdateTask.IsCompleted && !_worldUpdateTask.IsFaulted && !_worldUpdateTask.IsCanceled);

        if ((_lastPlayerChunk == null || currentChunk != _lastPlayerChunk.Value) && canStartNew)
        {
            _lastPlayerChunk = currentChunk;
            var renderDistance = _hudManager.Settings.RenderDistance;

            _worldUpdateTask = Task.Run(async () =>
            {
                try
                {
                    await _world.GenerateAsync(renderDistance, playerPos);
                    _world.UnloadChunks(playerPos, renderDistance + 1); // +1 to give some buffer
                }
                catch (Exception ex)
                {
                    LogWorldUpdateTaskFailed(ex);
                    // Don't rethrow - allow game to continue with current chunks
                }
            });
        }
    }

    private void OnRender(double deltaTime)
    {
        if (_window is null || _renderPipeline is null || _camera is null || _gl is null || _hudManager is null)
        {
            return;
        }

        _lifecycleManager.Render(deltaTime);

        var alpha = (float)(_accumulator / FixedDeltaTime);

        var lights = _lightSystem.GetActivePointLights().ToArray();

        var cameraPosition = (_camera as FirstPersonCamera)?.GetInterpolatedPosition(alpha) ?? _camera.Position;
        var block = _world.GetBlock((int)Math.Floor(cameraPosition.X), (int)Math.Floor(cameraPosition.Y), (int)Math.Floor(cameraPosition.Z));

        // We calculate this separately from the player controller's IsUnderwater to use the 
        // interpolated camera position for visual smoothness and to support different camera types.
        var isUnderwater = block.IsFluid;

        var fogColor = isUnderwater ? new Vector3(0.0f, 0.4f, 0.8f) : new Vector3(0.53f, 0.81f, 0.92f);

        // Apply sun lighting to fog/clear color
        if (!isUnderwater)
        {
            var sunColor = _lightSystem.Sun.Color * _lightSystem.Sun.Intensity;
            var sunDir = Vector3.Normalize(-_lightSystem.Sun.Direction);
            var dotL = Math.Clamp(Vector3.Dot(Vector3.UnitY, sunDir), 0.0f, 1.0f);

            // Blend between a dark night color and the sun-lit color
            var ambientLight = new Vector3(0.01f); // Darker constant ambient for night
            fogColor *= Vector3.Lerp(ambientLight, sunColor, dotL);
        }

        _gl.ClearColor(fogColor.X, fogColor.Y, fogColor.Z, 1.0f);

        var viewDistance = _world.Size * World.ChunkSize;
        var context = new RenderContext(
            View: _camera.GetViewMatrix(alpha),
            Projection: _camera.GetProjectionMatrix((float)_window.Size.X / _window.Size.Y),
            CameraPosition: cameraPosition,
            FogColor: fogColor,
            FogNear: isUnderwater ? 0.0f : viewDistance * _hudManager.Settings.FogNearFactor,
            FogFar: isUnderwater ? 20.0f : viewDistance * _hudManager.Settings.FogFarFactor,
            ScreenWidth: _window.Size.X,
            ScreenHeight: _window.Size.Y,
            UseNormalMap: _hudManager.Settings.UseNormalMap,
            NormalStrength: _hudManager.Settings.NormalStrength,
            UseAoMap: _hudManager.Settings.UseAoMap,
            AoMapStrength: _hudManager.Settings.AoMapStrength,
            UseMetallicMap: _hudManager.Settings.UseMetallicMap,
            MetallicStrength: _hudManager.Settings.MetallicStrength,
            UseRoughnessMap: _hudManager.Settings.UseRoughnessMap,
            RoughnessStrength: _hudManager.Settings.RoughnessStrength,
            Sun: new DirectionalLightData(_lightSystem.Sun.Direction, _lightSystem.Sun.Color, _lightSystem.Sun.Intensity),
            PointLights: lights,
            Exposure: _hudManager.Settings.Exposure,
            AutoExposureKey: _hudManager.Settings.AutoExposureKey,
            AutoExposureMin: _hudManager.Settings.AutoExposureMin,
            AutoExposureMax: _hudManager.Settings.AutoExposureMax,
            AutoExposureSpeed: _hudManager.Settings.AutoExposureSpeed,
            Gamma: _hudManager.Settings.Gamma,
            IsUnderwater: isUnderwater,
            Time: _worldTime?.Time ?? 0f,
            UseIbl: _hudManager.Settings.UseIbl,
            UseSsao: _hudManager.Settings.UseSsao,
            SsaoRadius: _hudManager.Settings.SsaoRadius,
            SsaoIntensity: _hudManager.Settings.SsaoIntensity,
            UseSsr: _hudManager.Settings.UseSsr,
            UseContactShadows: _hudManager.Settings.UseContactShadows,
            AtmosphereRayleighScale: _postProcessingRenderer?.RayleighScale ?? 1.0f,
            AtmosphereMieScale: _postProcessingRenderer?.MieScale ?? 1.0f,
            AtmosphereOzoneScale: _postProcessingRenderer?.OzoneScale ?? 1.0f,
            AtmosphereMieG: _postProcessingRenderer?.ScatteringG ?? 0.8f
        );

        _renderPipeline.Execute(_world, context);
        _hudManager?.Render((float)deltaTime, _world, _playerController, _renderPipeline.MeshManager, _lightSystem, _postProcessingRenderer, _sdk, _mods, _avatarLoader, _diagnosticsManager);
    }

    private void InitializeGraphicsState()
    {
        if (_gl == null)
        {
            return;
        }

        _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.CullFace);
        _gl.ClearColor(0.53f, 0.81f, 0.92f, 1.0f);

        // Reversed-Z default depth policy (research §1, §12.2): near→1, far→0, so the main
        // passes clear depth to 0.0 and keep what is GREATER. Passes that use a conventional
        // (non-reversed) projection — currently only the shadow map — override this locally and
        // restore it afterwards.
        _gl.ClearDepth(0.0f);
        _gl.DepthFunc(DepthFunction.Greater);
    }

#if DEBUG
    private DebugProc? _glDebugProc;

    private unsafe void InitializeGlDebugOutput()
    {
        if (_gl == null)
        {
            return;
        }

        _gl.Enable(EnableCap.DebugOutput);
        _gl.Enable(EnableCap.DebugOutputSynchronous);
        // Keep a field reference so the delegate isn't collected while the driver holds it.
        _glDebugProc = OnGlDebugMessage;
        _gl.DebugMessageCallback(_glDebugProc, null);
    }

    private void OnGlDebugMessage(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userParam)
    {
        if (severity == GLEnum.DebugSeverityNotification)
        {
            return;
        }

        var text = length > 0
            ? Marshal.PtrToStringAnsi(message, length)
            : Marshal.PtrToStringAnsi(message);

        if (type == GLEnum.DebugTypePerformance)
        {
            LogGlInfo(source, type, id, text);
            return;
        }

        switch (severity)
        {
            case GLEnum.DebugSeverityHigh:
                LogGlError(source, type, id, text);
                break;
            case GLEnum.DebugSeverityMedium:
                LogGlWarning(source, type, id, text);
                break;
            default:
                LogGlInfo(source, type, id, text);
                break;
        }
    }
#endif

    private void InitializeSystems()
    {
        if (_gl == null || _window == null)
        {
            return;
        }

        var inputContext = _window.CreateInput();
        _input = new InputManager(inputContext);
        _inputProvider = new KeyboardMouseInputProvider(inputContext);


        if (_sdk.Huds is not HudRegistry hudRegistry)
        {
            throw new InvalidOperationException("Huds registry must be a HudRegistry.");
        }

        _hudManager = new HudManager(_gl, _window, inputContext, hudRegistry);
        _hudManager.Initialize();

        _avatarLoader = new AvatarLoader(_gl);
        _avatarLoader.LoadSteamAvatar().Wait();
        _diagnosticsManager = new DiagnosticsManager();
        _hudManager.Settings.OnVisibilityChanged += () =>
        {
            // Force a world update when settings are closed, in case RenderDistance changed
            if (!_hudManager.Settings.IsVisible)
            {
                _lastPlayerChunk = null;
            }
        };

        // Initialize Assets and Atlas
        _atlas = new TextureAtlas(_gl, _sdk.Assets);
        _atlas.Build();

        var blockAtlas = new BlockAtlas(_atlas, _sdk.Blocks);
        var meshManager = new ChunkMeshManager(_world, blockAtlas.ResolveUvs, _loggerFactory.CreateLogger<ChunkMeshManager>());
        var physics = new PhysicsSystem(_world);
        var entity = new PhysicsEntity(new Transform { Position = new Vector3(1, 65, -6) }, physics);

        _camera = new FirstPersonCamera(entity, Vector3.UnitY * 1.6f);
        _playerController = new LocalPlayerController(entity, _camera, _world, _inputProvider, _sdk.Blocks);

        var cache = new ChunkRenderCache(_gl);
        _mainShader = new ShaderProgram(_gl, Shaders.DefaultVertex, Shaders.DefaultFragment);
        var terrainRenderer = new TerrainRenderer(_gl, cache, meshManager, _atlas, _mainShader);
        var waterRenderer = new WaterRenderer(_gl, cache, meshManager, _atlas);
        _torchRenderer = new TorchRenderer(_gl);
        _postProcessingRenderer = new PostProcessingRenderer(_gl);

        _renderPipeline = new DefaultRenderPipeline(_gl, cache, meshManager, terrainRenderer, waterRenderer, _torchRenderer, _postProcessingRenderer);

        _worldTime = new WorldTime { DayDurationInMinutes = 5f };
        _lightSystem.WorldTime = _worldTime;
        _sun = new Sun(_worldTime, _lightSystem);

        if (_input != null)
        {
            _lifecycleManager.Register(_input);
        }

        if (_hudManager != null)
        {
            _lifecycleManager.Register(_hudManager);
        }

        _lifecycleManager.Register(_worldTime);
        _lifecycleManager.Register(_sun);

        _lifecycleManager.Start();
    }

    private void RegisterInputHandlers()
    {
        if (_input == null)
        {
            return;
        }

        _input.Mouse.Cursor.CursorMode = CursorMode.Raw;
        _input.Keyboard.KeyUp += (_, key, _) => HandleGlobalKeys(key);
    }

    private void HandleGlobalKeys(Key key)
    {
        if (_input == null)
        {
            return;
        }

        if (key == Key.Escape)
        {
            _window?.Close();
        }

        if (key == Key.T)
        {
            PlaceTorch();
        }

        var shift = _input.Keyboard.IsKeyPressed(Key.ShiftLeft) || _input.Keyboard.IsKeyPressed(Key.ShiftRight);

        if (shift)
        {
            switch (key)
            {
                case Key.N:
                    _useNormalMap = !_useNormalMap;
                    LogNormalMappingToggledState(_useNormalMap);
                    break;
                case Key.M:
                    _useMetallicMap = !_useMetallicMap;
                    LogMetallicMappingToggledState(_useMetallicMap);
                    break;
                case Key.R:
                    _useRoughnessMap = !_useRoughnessMap;
                    LogRoughnessMappingToggledState(_useRoughnessMap);
                    break;
                case Key.L:
                    _useAoMap = !_useAoMap;
                    LogAoMappingToggledState(_useAoMap);
                    break;
            }
        }
    }

    /// <summary>
    /// Places a torch on the block the player is currently standing on: a 3D torch model rises from
    /// the block's top face, and a warm point light is registered so it illuminates the surroundings.
    /// </summary>
    private void PlaceTorch()
    {
        if (_torchRenderer == null || _playerController == null)
        {
            return;
        }

        // Only place on a real surface — not mid-jump, and not over air/water/lava.
        if (!_playerController.BlockBelow.IsSolid || _playerController.IsSwimming || _playerController.IsUnderwater)
        {
            return;
        }

        var p = _playerController.Entity.Position;

        // The supporting block's top face sits at floor(feetY); centre the torch on that block column.
        var basePos = new Vector3(
            MathF.Floor(p.X) + 0.5f,
            MathF.Floor(p.Y),
            MathF.Floor(p.Z) + 0.5f);

        _torchRenderer.AddTorch(basePos);

        _lightSystem.AddPointLight(new PointLightData(
            Position: basePos + new Vector3(0f, 0.55f, 0f), // at the flame
            Color: new Vector3(1.0f, 0.55f, 0.2f),
            Intensity: 3.0f,
            Constant: 1.0f,
            Linear: 0.18f,
            Quadratic: 0.10f));

        LogTorchPlaced(basePos.X, basePos.Y, basePos.Z, _torchRenderer.Count);
    }

    public void Run()
    {
        _window?.Run();
    }

    private void Exit() => _window?.Close();

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
                // Ensure OpenGL context is current during disposal
                // but only if the window still has a valid context
                if (_window?.GLContext != null)
                {
                    try
                    {
                        _window.MakeCurrent();
                    }
                    catch (Exception e)
                    {
                        LogMakeCurrentFailed(e);
                    }
                }

                _renderPipeline?.Dispose();
                _mainShader?.Dispose();
                _hudManager?.Dispose();
                _input?.Dispose();

                // We do NOT dispose _window here because it's owned by Program.cs
                // and managed via a using block there.
            }

            _disposed = true;
        }
    }

    private bool _disposed;
}