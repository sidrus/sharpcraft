using System.Numerics;
using Microsoft.Extensions.Logging;
using SharpCraft.Core;
using SharpCraft.Core.Numerics;
using SharpCraft.Core.Physics;
using SharpCraft.Game.Controllers;
using SharpCraft.Game.Input;
using SharpCraft.Game.Rendering;
using SharpCraft.Game.Rendering.Cameras;
using SharpCraft.Game.Rendering.Lighting;
using SharpCraft.Game.UI;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Steamworks;

namespace SharpCraft.Game;

public partial class Game : IDisposable
{
    private readonly World _world;
    private readonly IWindow? _window;
    private GL? _gl;

    private readonly ILogger<Game> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private InputManager? _input;
    private HudManager? _hudManager;
    private readonly LightingSystem _lightSystem = new();
    private IRenderPipeline? _renderPipeline;
    private ICamera? _camera;
    private LocalPlayerController? _playerController;
    private bool _useNormalMap = true;
    private bool _useAoMap = true;
    private bool _useSpecularMap = true;

    private const double FixedDeltaTime = 1.0 / 60.0;
    private double _accumulator;
    private Vector2<int>? _lastPlayerChunk;
    private Task? _worldUpdateTask;

    public Game(IWindow window, World world, ILoggerFactory loggerFactory)
    {
        _world = world;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<Game>();

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

    private async void OnLoad()
    {
        try
        {
            await OnLoadAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load game");
            Exit();
        }
    }

    private async Task OnLoadAsync()
    {
        LogCreatingOpenglContext();
        _gl = _window.CreateOpenGL();

        if (SteamClient.IsValid)
        {
            SteamFriends.SetRichPresence("status", "Exploring SharpCraft");
        }

        InitializeGraphicsState();
        await InitializeSystems();
        RegisterInputHandlers();
    }

    private void OnUpdate(double deltaTime)
    {
        SteamClient.RunCallbacks();

        _accumulator += deltaTime;

        while (_accumulator >= FixedDeltaTime)
        {
            if (_input?.Mouse.Cursor.CursorMode == CursorMode.Raw)
            {
                _playerController?.Update((float)FixedDeltaTime, _input.Keyboard);
            }

            _accumulator -= FixedDeltaTime;
        }

        UpdateWorldChunks();

        _hudManager?.OnUpdate(deltaTime);
        _input?.PostUpdate();
    }

    private void UpdateWorldChunks()
    {
        if (_playerController == null || _hudManager?.Settings == null) return;

        var playerPos = _playerController.Entity.Position;
        var currentChunkX = (int)Math.Floor(playerPos.X / World.ChunkSize);
        var currentChunkZ = (int)Math.Floor(playerPos.Z / World.ChunkSize);
        var currentChunk = new Vector2<int>(currentChunkX, currentChunkZ);

        if ((_lastPlayerChunk == null || currentChunk != _lastPlayerChunk.Value) && (_worldUpdateTask == null || _worldUpdateTask.IsCompleted))
        {
            _lastPlayerChunk = currentChunk;
            var renderDistance = _hudManager.Settings.RenderDistance;
                
            _worldUpdateTask = Task.Run(async () =>
            {
                await _world.GenerateAsync(renderDistance, playerPos);
                _world.UnloadChunks(playerPos, renderDistance + 1); // +1 to give some buffer
            });
        }
    }

    private void OnRender(double deltaTime)
    {
        if (_window is null || _renderPipeline is null || _camera is null || _gl is null)
        {
            return;
        }

        var alpha = (float)(_accumulator / FixedDeltaTime);

        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var lights = _lightSystem.GetActivePointLights()
            .Select(l => new PointLightData(l.Position, l.Color, l.Intensity, l.Constant, l.Linear, l.Quadratic))
            .ToArray();

        var viewDistance = _world.Size * World.ChunkSize;
        var context = new RenderContext(
            View: _camera.GetViewMatrix(alpha),
            Projection: _camera.GetProjectionMatrix((float)_window.Size.X / _window.Size.Y),
            CameraPosition: (_camera as FirstPersonCamera)?.GetInterpolatedPosition(alpha) ?? _camera.Position,
            FogColor: new Vector3(0.53f, 0.81f, 0.92f),
            FogNear: viewDistance * _hudManager.Settings.FogNearFactor,
            FogFar: viewDistance * _hudManager.Settings.FogFarFactor,
            ScreenWidth: _window.Size.X,
            ScreenHeight: _window.Size.Y,
            UseNormalMap: _hudManager.Settings.UseNormalMap,
            NormalStrength: _hudManager.Settings.NormalStrength,
            UseAoMap: _hudManager.Settings.UseAoMap,
            AoMapStrength: _hudManager.Settings.AoMapStrength,
            UseSpecularMap: _hudManager.Settings.UseSpecularMap,
            SpecularMapStrength: _hudManager.Settings.SpecularMapStrength,
            PointLights: lights,
            Exposure: _hudManager.Settings.Exposure,
            Gamma: _hudManager.Settings.Gamma
        );

        _renderPipeline.Execute(_world, context);
        _hudManager.Render((float)deltaTime, _world, _playerController);
    }

    private void InitializeGraphicsState()
    {
        if (_gl == null) return;
        _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.CullFace);
        _gl.ClearColor(0.53f, 0.81f, 0.92f, 1.0f);
    }

    private async Task InitializeSystems()
    {
        if (_gl == null || _window == null) return;

        var inputContext = _window.CreateInput();
        _input = new InputManager(inputContext);
        _hudManager = new HudManager(_gl, _window, inputContext, _loggerFactory.CreateLogger<HudManager>());
        _hudManager.OnCursorModeChanged += () => _playerController?.ResetMouse();
        await _hudManager.InitializeAsync();

        if (_hudManager.Settings != null)
        {
            _hudManager.Settings.OnVisibilityChanged += () =>
            {
                // Force a world update when settings are closed, in case RenderDistance changed
                if (!_hudManager.Settings.IsVisible)
                {
                    _lastPlayerChunk = null; 
                }
            };
        }

        var physics = new PhysicsSystem(_world);
        var entity = new PhysicsEntity(new Transform { Position = new Vector3(0, 80, 0) }, physics);

        _camera = new FirstPersonCamera(entity, Vector3.UnitY * 1.6f);
        _playerController = new LocalPlayerController(entity, _camera, _world);
        _renderPipeline = new DefaultRenderPipeline(_gl, _world);

        _lightSystem.AddPointLight(new PointLight
        {
            Position = new Vector3(3, 66, -10),
            Color = new Vector3(1.0f, 0.6f, 0.1f),
            Intensity = 5f
        });
        _lightSystem.AddPointLight(new PointLight
        {
            Position = _camera.Position,
            Color = new Vector3(1.0f, 1.0f, 1.0f)
        });
    }

    private void RegisterInputHandlers()
    {
        if (_input == null) return;

        _input.Mouse.MouseMove += (_, pos) =>
        {
            // Only allow the player controller to rotate the camera/player
            // if we are in Raw mouse mode (gameplay mode)
            if (_input.Mouse.Cursor.CursorMode == CursorMode.Raw)
            {
                _playerController?.HandleMouse(_input.Mouse, pos);
            }
        };

        _input.Mouse.Cursor.CursorMode = CursorMode.Raw;
        _input.Keyboard.KeyUp += (_, key, _) => HandleGlobalKeys(key);
    }

    private void HandleGlobalKeys(Key key)
    {
        if (_input == null) return;
        if (key == Key.Escape)
        {
            _window?.Close();
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
                case Key.H:
                    _useSpecularMap = !_useSpecularMap;
                    LogSpecularMappingToggledState(_useSpecularMap);
                    break;
                case Key.L:
                    _useAoMap = !_useAoMap;
                    LogAoMappingToggledState(_useAoMap);
                    break;
            }
        }
    }

    public void Run() => _window?.Run();

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
                    catch
                    {
                        // Ignore errors during MakeCurrent in disposal
                    }
                }

                _renderPipeline?.Dispose();
                _hudManager?.Dispose();
                _input?.Dispose();
                
                // We do NOT dispose _window here because it's owned by Program.cs
                // and managed via a using block there.
            }

            _disposed = true;
        }
    }

    private bool _disposed;

    [LoggerMessage(LogLevel.Information, "Creating OpenGL context...")]
    partial void LogCreatingOpenglContext();

    [LoggerMessage(LogLevel.Information, "Normal mapping toggled: {state}")]
    partial void LogNormalMappingToggledState(bool state);

    [LoggerMessage(LogLevel.Information, "Specular mapping toggled: {state}")]
    partial void LogSpecularMappingToggledState(bool state);

    [LoggerMessage(LogLevel.Information, "AO mapping toggled: {state}")]
    partial void LogAoMappingToggledState(bool state);
}