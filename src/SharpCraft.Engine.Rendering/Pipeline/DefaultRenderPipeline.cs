using SharpCraft.Engine.Rendering.IBL;
using SharpCraft.Engine.Rendering.Lighting;
using SharpCraft.Engine.Rendering.Shaders;
using System.Numerics;

namespace SharpCraft.Engine.Rendering.Pipeline;

/// <summary>
/// Minimal forward pipeline (research §11 foundation): a shadow pass, then an HDR-linear forward
/// pass (sky, opaque terrain, sun, transparent water) into an fp16 buffer, then the SDR output
/// transform. Reversed-Z throughout (§12.2). Clustered light culling, IBL, GTAO, bloom and the
/// rest of the §11 build order are added in later rounds.
/// </summary>
public class DefaultRenderPipeline(
    GL gl,
    ChunkRenderCache cache,
    ChunkMeshManager meshManager,
    TerrainRenderer terrainRenderer,
    WaterRenderer waterRenderer,
    TorchRenderer torchRenderer,
    PostProcessingRenderer postProcessingRenderer)
    : IDisposable
{
    public ChunkMeshManager MeshManager { get; } = meshManager;

    private Framebuffer? _framebuffer;
    private ClusteredLighting? _clustered;
    private AutoExposure? _autoExposure;
    private TemporalAa? _taa;
    private Matrix4x4 _mainViewProj;
    private Matrix4x4 _mainProjection;
    private int _lastWidth;
    private int _lastHeight;

    private readonly RenderTargets _targets = new();

    private readonly UniformBufferObject<SceneData> _sceneUbo = new(gl, 0);
    private readonly UniformBufferObject<LightingData> _lightingUbo = new(gl, 1);
    private readonly UniformBufferObject<CsmData> _csmUbo = new(gl, 2);
    private readonly SunPass _sunPass = new(gl);
    private readonly SkyboxPass _skyboxPass = new(gl);
    private readonly BloomPass _bloomPass = new(gl, postProcessingRenderer);
    private readonly OutputPass _outputPass = new(postProcessingRenderer);
    private readonly VolumetricPass _volumetricPass = new(gl, postProcessingRenderer, ShadowDistance);
    private readonly IblPass _iblPass = new(gl);
    private readonly DepthPrepassPass _depthPrepassPass = new(gl, cache);
    private readonly GtaoPass _gtaoPass = new(gl);
    private readonly ShadowPass _shadowPass = new(gl, cache, ShadowMapSize, CascadeCount);
    private readonly TerrainPass _terrainPass = new(terrainRenderer, torchRenderer);
    private readonly SsrSnapshotPass _ssrSnapshotPass = new(gl);
    private readonly WaterPass _waterPass = new(gl, waterRenderer);

    private RenderPassPipeline? _pipeline;

    // Cascaded shadow map config (research §8).
    private const int CascadeCount = 8;
    private const uint ShadowMapSize = 2048;
    private const float ShadowDistance = 300.0f;

    private ShadowCascades.Result _cascades;

    public void Execute(IWorld world, RenderContext context)
    {
        if (context.Camera.ScreenWidth <= 0 || context.Camera.ScreenHeight <= 0)
        {
            return;
        }

        _pipeline ??= BuildPipeline();
        _targets.Reset();

        if (_framebuffer == null || _lastWidth != context.Camera.ScreenWidth || _lastHeight != context.Camera.ScreenHeight)
        {
            _framebuffer?.Dispose();
            _framebuffer = new Framebuffer(gl, context.Camera.ScreenWidth, context.Camera.ScreenHeight, hdr: true);
            _lastWidth = context.Camera.ScreenWidth;
            _lastHeight = context.Camera.ScreenHeight;
        }

        _targets.HdrSceneFbo = _framebuffer.Handle;
        _targets.HdrSceneDepth = _framebuffer.DepthTextureHandle;

        // Temporal AA (research §9): jitter the main-pass projection sub-pixel each frame. The
        // unjittered View/Projection are still used for shadows, clustering and culling.
        _taa ??= new TemporalAa(gl);
        var mainProjection = context.Effects.UseTaa
            ? _taa.ApplyJitter(context.Camera.Projection, context.Camera.ScreenWidth, context.Camera.ScreenHeight)
            : context.Camera.Projection;
        _mainProjection = mainProjection;
        _mainViewProj = context.Camera.View * mainProjection;
        _targets.MainProjection = _mainProjection;
        _targets.MainViewProj = _mainViewProj;

        cache.Update(world.GetLoadedChunks());
        UpdateUbos(context);

        // Image-based lighting bake (research §4.2/§6): refresh the sky env / irradiance /
        // prefilter when the sun moves, and expose the maps to the forward pass. Throttled
        // internally, so most frames this is a no-op.
        if (_iblPass.Enabled(context))
        {
            _iblPass.Execute(world, context, _targets);
        }

        // Clustered forward+ light culling (research §2): build the froxel grid + cull lights into
        // SSBOs via compute, before any geometry is drawn. The forward terrain pass then shades
        // only each fragment's cluster lights.
        _clustered ??= new ClusteredLighting(gl);
        _clustered.Update(context.Camera.View, context.Camera.Projection, context.Camera.ScreenWidth, context.Camera.ScreenHeight,
            context.Lighting.PointLights ?? []);

        var width = (uint)context.Camera.ScreenWidth;
        var height = (uint)context.Camera.ScreenHeight;

        // === SHADOW PASS (cascaded, research §8) ===
        _shadowPass.Execute(world, context, _targets);

        // === DEPTH PRE-PASS + GTAO (research §7) ===
        // Render opaque terrain depth at screen res (reuses the shadow shader with the main VP). It
        // feeds GTAO, SSR, and contact shadows (the opaque scene depth they all march against).
        if (_depthPrepassPass.Enabled(context))
        {
            _depthPrepassPass.Execute(world, context, _targets);
        }
        if (_gtaoPass.Enabled(context))
        {
            _gtaoPass.Execute(world, context, _targets);
        }

        // Inverse of the main (jittered) VP — used to reconstruct world position from depth in SSR.
        Matrix4x4.Invert(_mainViewProj, out var invMainViewProj);
        _targets.InvViewProj = invMainViewProj;

        // === MAIN HDR FORWARD PASS ===
        _framebuffer.Bind();
        gl.Viewport(0, 0, width, height);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        gl.Enable(EnableCap.CullFace);
        gl.Disable(EnableCap.Blend);

        // Sky fills the background (depth test off, no depth write).
        _skyboxPass.Execute(world, context, _targets);

        // Opaque, forward-lit terrain + torch models. Bind the clustered light buffers for shading.
        _clustered.BindForShading();
        _terrainPass.Execute(world, context, _targets);

        // Sun disc at the far plane (occluded by terrain via GEqual).
        _sunPass.Execute(world, context, _targets);

        // Snapshot the opaque HDR scene so the water SSR pass can sample it (a surface can't read
        // the color attachment it is drawing into).
        if (_ssrSnapshotPass.Enabled(context))
        {
            _ssrSnapshotPass.Execute(world, context, _targets);
        }

        // Transparent water (forward, blended). Binds the clustered light buffers, then the pass owns
        // its blend/cull/depth-write state.
        _clustered.BindForShading();
        _waterPass.Execute(world, context, _targets);

        _framebuffer.Unbind();

        // === VOLUMETRICS (research §11 step 10 / §8) ===
        // Half-res ray-march of height fog + sun shafts against the CSM, then composite into the HDR
        // scene (scene·T + inscatter) BEFORE the TAA resolve, so the half-res march noise is cleaned
        // up temporally. Marches against the main pass depth + the (already-current) CsmData UBO.
        if (_volumetricPass.Enabled(context))
        {
            _volumetricPass.Execute(world, context, _targets);
        }

        // === TEMPORAL AA RESOLVE (research §9) ===
        // Reproject + blend history into this frame; downstream passes read the resolved HDR.
        var sceneTexture = _framebuffer.TextureHandle;
        if (context.Effects.UseTaa)
        {
            sceneTexture = _taa.Resolve(
                _framebuffer.TextureHandle, _framebuffer.DepthTextureHandle,
                _mainViewProj, context.Camera.ScreenWidth, context.Camera.ScreenHeight);
        }
        _targets.ResolvedScene = sceneTexture;

        // === AUTO-EXPOSURE (research §5.2) ===
        // Histogram the resolved HDR radiance and adapt the exposure before the output transform.
        _autoExposure ??= new AutoExposure(gl);
        _autoExposure.Update(sceneTexture, context.Camera.ScreenWidth, context.Camera.ScreenHeight,
            context.Exposure.AutoExposureKey, context.Exposure.AutoExposureMin, context.Exposure.AutoExposureMax, context.Exposure.AutoExposureSpeed);

        // === BLOOM (research §5.6) ===
        // Build the dual-filter pyramid from the resolved HDR scene; composited in the output pass.
        if (_bloomPass.Enabled(context))
        {
            _bloomPass.Execute(world, context, _targets);
        }

        // === OUTPUT TRANSFORM (HDR fp16 → SDR sRGB) ===
        // TAA already resolved spatial aliasing, so skip the FXAA edge filter when it's on.
        _autoExposure.BindForOutput();
        _outputPass.Execute(world, context, _targets);
    }

    private RenderPassPipeline BuildPipeline()
    {
        return new RenderPassPipeline(
        [
            _iblPass, _shadowPass, _depthPrepassPass, _gtaoPass,
            _skyboxPass, _terrainPass, _sunPass, _ssrSnapshotPass, _waterPass,
            _volumetricPass, _bloomPass, _outputPass,
        ],
        new HashSet<RenderResource> { RenderResource.InvViewProj, RenderResource.ResolvedScene });
    }

    private void UpdateUbos(RenderContext context)
    {
        var lightDirection = context.Lighting.Sun?.Direction ?? Vector3.Normalize(new Vector3(0.8f, -0.5f, 0.1f));
        var lightColor = context.Lighting.Sun?.Color ?? new Vector3(1.0f, 0.95f, 0.8f);
        var lightIntensity = context.Lighting.Sun?.Intensity ?? 1.0f;

        // Cascaded shadow matrices: split the view frustum, fit + texel-snap each slice (§8).
        _cascades = ShadowCascades.Compute(
            context.Camera.View, context.Camera.Projection, lightDirection,
            ShadowDistance, ShadowMapSize, CascadeCount);
        _targets.CascadeLightMatrices = _cascades.LightSpaceMatrices;

        var m = _cascades.LightSpaceMatrices;
        var s = _cascades.SplitDepths;
        var csmData = new CsmData
        {
            LightSpaceMatrix0 = m[0],
            LightSpaceMatrix1 = m.Length > 1 ? m[1] : m[0],
            LightSpaceMatrix2 = m.Length > 2 ? m[2] : m[0],
            LightSpaceMatrix3 = m.Length > 3 ? m[3] : m[0],
            SplitDepths = new Vector4(
                s[0],
                s.Length > 1 ? s[1] : s[0],
                s.Length > 2 ? s[2] : s[0],
                s.Length > 3 ? s[3] : s[0]),
            Params = new Vector4(CascadeCount, ShadowMapSize, 0f, 0f)
        };
        _csmUbo.Update(csmData);

        var sceneData = new SceneData
        {
            ViewProjection = _mainViewProj, // jittered for TAA (research §9); View stays unjittered

            ViewPos = new Vector4(context.Camera.CameraPosition, 1.0f),
            FogColor = new Vector4(context.Fog.FogColor, 1.0f),
            FogNear = context.Fog.FogNear,
            FogFar = context.Fog.FogFar,
            Exposure = context.Exposure.Exposure,
            Gamma = context.Exposure.Gamma,
            View = context.Camera.View
        };
        _sceneUbo.Update(sceneData);

        var lightingData = new LightingData
        {
            LightSpaceMatrix = _cascades.LightSpaceMatrices[0], // cascade 0 for water
            DirLight = new DirLightData
            {
                Direction = new Vector4(lightDirection, 0.0f),
                Color = new Vector4(lightColor * lightIntensity, 1.0f)
            }
        };

        _lightingUbo.Update(lightingData);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            (_pipeline ?? BuildPipeline()).Dispose();
            _sceneUbo.Dispose();
            _lightingUbo.Dispose();
            _csmUbo.Dispose();
            cache.Dispose();
            postProcessingRenderer.Dispose();
            _framebuffer?.Dispose();
            _clustered?.Dispose();
            _autoExposure?.Dispose();
            _taa?.Dispose();
        }
        _disposed = true;
    }

    private bool _disposed;
}