using System.Numerics;
using Microsoft.Extensions.Logging;
using SharpCraft.Core;
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

namespace SharpCraft.Game;

public partial class Game : IDisposable
{
    private readonly World _world;
    private readonly IWindow? _window;
    private GL? _gl;

    private readonly ILogger<Game> _logger;
    private InputManager? _input;
    private HudManager? _hudManager;
    private LightingSystem _lightSystem = new();
    private IRenderPipeline? _renderPipeline;
    private ICamera? _camera;
    private LocalPlayerController? _playerController;
    private bool _useNormalMap = true;
    private bool _useAoMap = true;
    private bool _useSpecularMap = true;

    public Game(IWindow window, World world, ILoggerFactory loggerFactory)
    {
        _world = world;
        _logger = loggerFactory.CreateLogger<Game>();

        _window = window;
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Resize += OnResize;
    }

    private void OnResize(Vector2D<int> size)
    {
        _gl?.Viewport(0, 0, (uint)size.X, (uint)size.Y);
    }

    private void OnLoad()
    {
        LogCreatingOpenglContext();
        _gl = _window.CreateOpenGL();

        InitializeGraphicsState();
        InitializeSystems();
        RegisterInputHandlers();
    }

    private void OnUpdate(double deltaTime)
    {
        if (_input?.Mouse.Cursor.CursorMode == CursorMode.Raw)
        {
            _playerController?.Update((float)deltaTime, _input.Keyboard);
        }

        _hudManager?.Update((float)deltaTime);
        _input?.PostUpdate();
    }

    private void OnRender(double deltaTime)
    {
        if (_window is null || _renderPipeline is null || _camera is null || _gl is null)
        {
            return;
        }

        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var lights = _lightSystem.GetActivePointLights()
            .Select(l => new PointLightData(l.Position, l.Color, l.Intensity, l.Constant, l.Linear, l.Quadratic))
            .ToArray();

        var viewDistance = _world.Size * _world.ChunkSize;
        var context = new RenderContext(
            View: _camera.GetViewMatrix(),
            Projection: _camera.GetProjectionMatrix((float)_window.Size.X / _window.Size.Y),
            CameraPosition: _camera.Position,
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

    private void InitializeSystems()
    {
        if (_gl == null || _window == null) return;

        var inputContext = _window.CreateInput();
        _input = new InputManager(inputContext);
        _hudManager = new HudManager(_gl, _window, inputContext);
        _hudManager.OnCursorModeChanged += () => _playerController?.ResetMouse();

        var physics = new PhysicsSystem(_world);
        var entity = new PhysicsEntity(new Transform { Position = new Vector3(0, 80, 0) }, physics);

        _camera = new FirstPersonCamera(entity, Vector3.UnitY * 1.6f);
        _playerController = new LocalPlayerController(entity, _camera, _world);
        _renderPipeline = new DefaultRenderPipeline(_gl);

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
                _hudManager?.Dispose();
                _input?.Dispose();
                _window?.Dispose();
                _renderPipeline?.Dispose();
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