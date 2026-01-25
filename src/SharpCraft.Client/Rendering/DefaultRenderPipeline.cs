using SharpCraft.Client.Rendering.Shaders;
using SharpCraft.Engine.Universe;
using Silk.NET.OpenGL;
using System.Numerics;

namespace SharpCraft.Client.Rendering;

public class DefaultRenderPipeline(
    GL gl,
    World world,
    ChunkRenderCache cache,
    ChunkMeshManager meshManager,
    TerrainRenderer terrainRenderer,
    WaterRenderer waterRenderer,
    PostProcessingRenderer postProcessingRenderer)
    : IRenderPipeline
{
    public ChunkMeshManager MeshManager { get; } = meshManager;

    private World? _world = world;
    private RenderContext? _context;
    private Framebuffer? _framebuffer;
    private GBuffer? _gBuffer;
    private ShadowMap? _shadowMap;
    private ShadowMapRenderer? _shadowMapRenderer;
    private DeferredLightingRenderer? _deferredLightingRenderer;
    private int _lastWidth;
    private int _lastHeight;

    private readonly UniformBufferObject<SceneData> _sceneUbo = new(gl, 0);
    private readonly UniformBufferObject<LightingData> _lightingUbo = new(gl, 1);
    private readonly SunRenderer _sunRenderer = new(gl);
    private readonly SkyboxRenderer _skyboxRenderer = new(gl);

    private Matrix4x4 _lightSpaceMatrix;

    public void OnRender(double deltaTime)
    {
        if (_world == null || !_context.HasValue) return;
        Execute(_world, _context.Value);
    }

    public void SetContext(World world, RenderContext context)
    {
        _world = world;
        _context = context;
    }

    public void Execute(World world, RenderContext context)
    {
        if (context.ScreenWidth <= 0 || context.ScreenHeight <= 0) return;

        // Initialize or resize framebuffers
        if (_framebuffer == null || _lastWidth != context.ScreenWidth || _lastHeight != context.ScreenHeight)
        {
            _framebuffer?.Dispose();
            _gBuffer?.Dispose();
            _framebuffer = new Framebuffer(gl, context.ScreenWidth, context.ScreenHeight, true);
            _gBuffer = new GBuffer(gl, context.ScreenWidth, context.ScreenHeight);
            _lastWidth = context.ScreenWidth;
            _lastHeight = context.ScreenHeight;
        }

        if (_shadowMap == null)
        {
            _shadowMap = new ShadowMap(gl, 4096, 4096);
            _shadowMapRenderer = new ShadowMapRenderer(gl, cache, new ShaderProgram(gl, Shaders.Shaders.ShadowVertex, Shaders.Shaders.ShadowFragment));
        }

        _deferredLightingRenderer ??= new DeferredLightingRenderer(gl);

        // Update the cache for the entire frame
        var activeChunks = world.GetLoadedChunks();
        cache.Update(activeChunks);

        UpdateUbos(context);

        // === SHADOW PASS ===
        gl.Enable(EnableCap.DepthTest);
        gl.CullFace(TriangleFace.Front);
        _shadowMap.Bind();
        gl.Clear(ClearBufferMask.DepthBufferBit);
        _shadowMapRenderer!.Render(world, _lightSpaceMatrix);
        _shadowMap.Unbind();
        gl.CullFace(TriangleFace.Back);
        gl.Viewport(0, 0, (uint)context.ScreenWidth, (uint)context.ScreenHeight);

        // === G-BUFFER PASS (Deferred Geometry) ===
        _gBuffer!.Bind();
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.CullFace);
        gl.Disable(EnableCap.Blend);

        // Render opaque geometry to G-Buffer (outputs material properties, not lit result)
        terrainRenderer.Render(world, context with { ShadowMap = _shadowMap.DepthMap });
        _gBuffer.Unbind();

        // === DEFERRED LIGHTING PASS ===
        _framebuffer.Bind();
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Render Skybox first (will be visible where G-Buffer has no geometry)
        gl.Disable(EnableCap.DepthTest);
        _skyboxRenderer.Render(context);

        // Deferred lighting: read G-Buffer and compute lighting
        _deferredLightingRenderer.Render(_gBuffer, _shadowMap.DepthMap, context);

        // Re-enable depth test and copy G-Buffer depth for transparent pass
        gl.Enable(EnableCap.DepthTest);

        // Blit depth from G-Buffer to main framebuffer for correct transparency depth testing
        BlitDepth(_gBuffer, _framebuffer, context.ScreenWidth, context.ScreenHeight);

        // Sun Pass
        _sunRenderer.Render(context);

        // === TRANSPARENT PASS (Forward Rendering) ===
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        gl.Disable(EnableCap.CullFace);
        waterRenderer.Render(world, context);

        _framebuffer.Unbind();
        
        var invViewProj = Matrix4x4.Identity;
        Matrix4x4.Invert(context.ViewProjection, out invViewProj);

        var lightDirection = context.Sun?.Direction ?? Vector3.Normalize(new Vector3(0.8f, -0.5f, 0.1f));
        var lightColor = context.Sun?.Color ?? new Vector3(1.0f, 0.95f, 0.8f);
        var lightIntensity = context.Sun?.Intensity ?? 1.0f;

        postProcessingRenderer.Render(
            _framebuffer.TextureHandle,
            _framebuffer.DepthTextureHandle,
            _shadowMap!.DepthMap,
            context.IsUnderwater,
            context.Time,
            context.ScreenWidth,
            context.ScreenHeight,
            invViewProj,
            _lightSpaceMatrix,
            -lightDirection,
            lightColor * lightIntensity,
            context.CameraPosition);
    }

    private void UpdateUbos(RenderContext context)
    {
        var lightDirection = context.Sun?.Direction ?? Vector3.Normalize(new Vector3(0.8f, -0.5f, 0.1f));
        var lightColor = context.Sun?.Color ?? new Vector3(1.0f, 0.95f, 0.8f);
        var lightIntensity = context.Sun?.Intensity ?? 1.0f;
        
        // Use a tighter and stable shadow frustum
        var size = 80.0f; 
        var lightProjection = Matrix4x4.CreateOrthographicOffCenter(-size, size, -size, size, 1.0f, 400.0f);
        var lightView = Matrix4x4.CreateLookAt(context.CameraPosition - lightDirection * 200.0f, context.CameraPosition, Vector3.UnitY);
        
        var shadowMatrix = lightView * lightProjection;

        // Stabilize shadow map by snapping to texel increments
        // This prevents the "shimmering" effect when moving the camera
        var shadowOrigin = Vector4.Transform(Vector3.Zero, shadowMatrix);
        shadowOrigin *= (_shadowMap?.Width ?? 4096) / 2.0f;

        var roundedOrigin = new Vector4(MathF.Round(shadowOrigin.X), MathF.Round(shadowOrigin.Y), shadowOrigin.Z, shadowOrigin.W);
        var roundOffset = (roundedOrigin - shadowOrigin) * 2.0f / (_shadowMap?.Width ?? 4096);
        roundOffset.Z = 0.0f;
        roundOffset.W = 0.0f;

        _lightSpaceMatrix = shadowMatrix * Matrix4x4.CreateTranslation(new Vector3(roundOffset.X, roundOffset.Y, roundOffset.Z));

        var sceneData = new SceneData
        {
            ViewProjection = context.ViewProjection,
            ViewPos = new Vector4(context.CameraPosition, 1.0f),
            FogColor = new Vector4(context.FogColor, 1.0f),
            FogNear = context.FogNear,
            FogFar = context.FogFar,
            Exposure = context.Exposure,
            Gamma = context.Gamma
        };
        _sceneUbo.Update(sceneData);

        var lightingData = new LightingData
        {
            LightSpaceMatrix = _lightSpaceMatrix,
            DirLight = new DirLightData
            {
                Direction = new Vector4(lightDirection, 0.0f),
                Color = new Vector4(lightColor * lightIntensity, 1.0f)
            },
            PointLight0 = DefaultPointLight(),
            PointLight1 = DefaultPointLight(),
            PointLight2 = DefaultPointLight(),
            PointLight3 = DefaultPointLight()
        };

        if (context.PointLights != null)
        {
            if (context.PointLights.Length > 0) lightingData.PointLight0 = MapLight(context.PointLights[0]);
            if (context.PointLights.Length > 1) lightingData.PointLight1 = MapLight(context.PointLights[1]);
            if (context.PointLights.Length > 2) lightingData.PointLight2 = MapLight(context.PointLights[2]);
            if (context.PointLights.Length > 3) lightingData.PointLight3 = MapLight(context.PointLights[3]);
        }

        _lightingUbo.Update(lightingData);
    }

    private static PointLightDataStd140 DefaultPointLight() => new()
    {
        Constant = 1.0f,
        Intensity = 0.0f
    };

    private static PointLightDataStd140 MapLight(Lighting.PointLightData light)
    {
        return new PointLightDataStd140
        {
            Position = new Vector4(light.Position, 1.0f),
            Color = new Vector4(light.Color, 1.0f),
            Intensity = light.Intensity,
            Constant = light.Constant,
            Linear = light.Linear,
            Quadratic = light.Quadratic
        };
    }

    private void BlitDepth(GBuffer source, Framebuffer destination, int width, int height)
    {
        // Read from G-Buffer framebuffer
        gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, GetFramebufferHandle(source));
        // Write to main framebuffer
        gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, GetFramebufferHandle(destination));
        
        gl.BlitFramebuffer(
            0, 0, width, height,
            0, 0, width, height,
            ClearBufferMask.DepthBufferBit,
            BlitFramebufferFilter.Nearest);
        
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, GetFramebufferHandle(destination));
    }

    private static uint GetFramebufferHandle(GBuffer gBuffer) => gBuffer.Handle;

    private static uint GetFramebufferHandle(Framebuffer framebuffer) => framebuffer.Handle;

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
                _sceneUbo.Dispose();
                _lightingUbo.Dispose();
                _sunRenderer.Dispose();
                _skyboxRenderer.Dispose();
                cache.Dispose();
                terrainRenderer.Dispose();
                waterRenderer.Dispose();
                postProcessingRenderer.Dispose();
                _framebuffer?.Dispose();
                _gBuffer?.Dispose();
                _shadowMap?.Dispose();
                _shadowMapRenderer?.Dispose();
                _deferredLightingRenderer?.Dispose();
            }

            _disposed = true;
        }
    }

    private bool _disposed;
}