using SharpCraft.Engine.Rendering.IBL;
using SharpCraft.Engine.Rendering.Lighting;
using SharpCraft.Engine.Rendering.Shaders;
using System.Numerics;

namespace SharpCraft.Engine.Rendering;

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
    private CascadedShadowMap? _csm;
    private ShadowMapRenderer? _shadowMapRenderer;
    private IblBaker? _iblBaker;
    private ClusteredLighting? _clustered;
    private AutoExposure? _autoExposure;
    private TemporalAa? _taa;
    private Framebuffer? _depthPrepass;
    private Framebuffer? _opaqueColor;
    private GtaoRenderer? _gtao;
    private BloomRenderer? _bloom;
    private VolumetricRenderer? _volumetric;
    private Matrix4x4 _mainViewProj;
    private Matrix4x4 _mainProjection;
    private int _lastWidth;
    private int _lastHeight;

    private readonly RenderTargets _targets = new();

    private readonly UniformBufferObject<SceneData> _sceneUbo = new(gl, 0);
    private readonly UniformBufferObject<LightingData> _lightingUbo = new(gl, 1);
    private readonly UniformBufferObject<CsmData> _csmUbo = new(gl, 2);
    private readonly SunRenderer _sunRenderer = new(gl);
    private readonly SkyboxRenderer _skyboxRenderer = new(gl);

    // Cascaded shadow map config (research §8).
    private const int CascadeCount = 4;
    private const uint ShadowMapSize = 2048;
    private const float ShadowDistance = 220.0f;

    private ShadowCascades.Result _cascades;

    public void Execute(IWorld world, RenderContext context)
    {
        if (context.ScreenWidth <= 0 || context.ScreenHeight <= 0) return;

        _targets.Reset();

        if (_framebuffer == null || _lastWidth != context.ScreenWidth || _lastHeight != context.ScreenHeight)
        {
            _framebuffer?.Dispose();
            _framebuffer = new Framebuffer(gl, context.ScreenWidth, context.ScreenHeight, hdr: true);
            _lastWidth = context.ScreenWidth;
            _lastHeight = context.ScreenHeight;
        }

        var csm = _csm;
        if (csm == null)
        {
            csm = new CascadedShadowMap(gl, ShadowMapSize, CascadeCount);
            _csm = csm;
            _shadowMapRenderer = new ShadowMapRenderer(gl, cache, new ShaderProgram(gl, Shaders.Shaders.ShadowVertex, Shaders.Shaders.ShadowFragment));
        }

        _targets.ShadowMap = csm.DepthArray;

        // Temporal AA (research §9): jitter the main-pass projection sub-pixel each frame. The
        // unjittered View/Projection are still used for shadows, clustering and culling.
        _taa ??= new TemporalAa(gl);
        var mainProjection = context.UseTaa
            ? _taa.ApplyJitter(context.Projection, context.ScreenWidth, context.ScreenHeight)
            : context.Projection;
        _mainProjection = mainProjection;
        _mainViewProj = context.View * mainProjection;

        cache.Update(world.GetLoadedChunks());
        UpdateUbos(context);

        // Image-based lighting bake (research §4.2/§6): refresh the sky env / irradiance /
        // prefilter when the sun moves, and expose the maps to the forward pass. Throttled
        // internally, so most frames this is a no-op.
        if (context.UseIbl)
        {
            _iblBaker ??= new IblBaker(gl);
            var lightDir = context.Sun?.Direction ?? Vector3.Normalize(new Vector3(0.8f, -0.5f, 0.1f));
            var toSun = Vector3.Normalize(-lightDir);
            var sunColor = context.Sun?.Color ?? new Vector3(1.0f, 0.95f, 0.8f);
            _iblBaker.Update(toSun, sunColor, context.AtmosphereMieG, captureIntensity: 4.0f);

            if (_iblBaker.IsReady)
            {
                _targets.IrradianceMap = _iblBaker.IrradianceMap;
                _targets.PrefilterMap = _iblBaker.PrefilterMap;
                _targets.BrdfLut = _iblBaker.BrdfLut;
            }
        }

        // Clustered forward+ light culling (research §2): build the froxel grid + cull lights into
        // SSBOs via compute, before any geometry is drawn. The forward terrain pass then shades
        // only each fragment's cluster lights.
        _clustered ??= new ClusteredLighting(gl);
        _clustered.Update(context.View, context.Projection, context.ScreenWidth, context.ScreenHeight,
            context.PointLights ?? []);

        var width = (uint)context.ScreenWidth;
        var height = (uint)context.ScreenHeight;

        // === SHADOW PASS (cascaded, research §8) ===
        // Conventional (non-reversed) ortho per cascade: clear to 1.0, keep LESS, clamp casters (§12.2).
        // Render BOTH faces (no culling): the light-facing surface wins the depth test, so shadows
        // anchor at the caster's near side instead of detaching by the occluder's thickness
        // (front-face culling on solid 1-block voxels caused ~1-block peter-panning). Normal-offset
        // bias in the shadow sampling handles the resulting self-shadow acne.
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.DepthClamp);
        gl.DepthFunc(DepthFunction.Less);
        gl.ClearDepth(1.0f);
        gl.Disable(EnableCap.CullFace);
        for (int c = 0; c < CascadeCount; c++)
        {
            csm.BindLayer(c);
            gl.Clear(ClearBufferMask.DepthBufferBit);
            _shadowMapRenderer?.Render(world, _cascades.LightSpaceMatrices[c]);
        }
        csm.Unbind();
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);

        // Restore the reversed-Z main-pass depth policy (near→1, far→0).
        gl.Disable(EnableCap.DepthClamp);
        gl.DepthFunc(DepthFunction.Greater);
        gl.ClearDepth(0.0f);

        // === DEPTH PRE-PASS + GTAO (research §7) ===
        // Render opaque terrain depth at screen res (reuses the shadow shader with the main VP). It
        // feeds GTAO, SSR, and contact shadows (the opaque scene depth they all march against).
        if (context.UseSsao || context.UseSsr || context.UseContactShadows)
        {
            if (_depthPrepass == null || _depthPrepass.Width != context.ScreenWidth || _depthPrepass.Height != context.ScreenHeight)
            {
                _depthPrepass?.Dispose();
                _depthPrepass = new Framebuffer(gl, context.ScreenWidth, context.ScreenHeight, hdr: false);
            }

            _depthPrepass.Bind();
            gl.Viewport(0, 0, width, height);
            gl.Clear(ClearBufferMask.DepthBufferBit);
            _shadowMapRenderer?.Render(world, _mainViewProj);
            _depthPrepass.Unbind();
            _targets.SceneDepthTexture = _depthPrepass.DepthTextureHandle;

            if (context.UseSsao)
            {
                _gtao ??= new GtaoRenderer(gl);
                _targets.GtaoTexture = _gtao.Render(_depthPrepass.DepthTextureHandle, _mainProjection,
                    context.ScreenWidth, context.ScreenHeight, context.SsaoRadius, context.SsaoIntensity);
            }
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
        gl.Disable(EnableCap.DepthTest);
        _skyboxRenderer.Render(context, _targets);
        gl.Enable(EnableCap.DepthTest);

        // Opaque, forward-lit terrain. Bind the clustered light buffers for the shading pass.
        _clustered.BindForShading();
        terrainRenderer.Render(world, context, _targets);

        // Placed torch models (opaque, forward-lit, emissive head). Drawn after terrain so they
        // depth-test against it.
        torchRenderer.Render(context);

        // Sun disc at the far plane (occluded by terrain via GEqual).
        _sunRenderer.Render(context);

        // Snapshot the opaque HDR scene so the water SSR pass can sample it (a surface can't read
        // the colour attachment it is drawing into). Reuses the pre-pass depth as the scene depth.
        if (context.UseSsr && _targets.SceneDepthTexture != 0)
        {
            if (_opaqueColor == null || _opaqueColor.Width != context.ScreenWidth || _opaqueColor.Height != context.ScreenHeight)
            {
                _opaqueColor?.Dispose();
                _opaqueColor = new Framebuffer(gl, context.ScreenWidth, context.ScreenHeight, hdr: true);
            }
            gl.BlitNamedFramebuffer(_framebuffer.Handle, _opaqueColor.Handle,
                0, 0, context.ScreenWidth, context.ScreenHeight,
                0, 0, context.ScreenWidth, context.ScreenHeight,
                ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
            _framebuffer.Bind();
            gl.Viewport(0, 0, width, height);
            _targets.OpaqueColorTexture = _opaqueColor.TextureHandle;
        }

        // Transparent water (forward, blended). The fix for the z-fighting spikes was depth-WRITE
        // off (test against the opaque scene, don't write). Culling stays OFF: the water mesh has no
        // bottom faces (culled against the lake bed at mesh time), so there's nothing to double-
        // blend, and culling would instead leave seams where edge faces face away.
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        gl.Disable(EnableCap.CullFace);
        gl.DepthMask(false);
        waterRenderer.Render(world, context, _targets);
        gl.DepthMask(true);
        gl.Enable(EnableCap.CullFace);
        gl.Disable(EnableCap.Blend);

        _framebuffer.Unbind();

        // === VOLUMETRICS (research §11 step 10 / §8) ===
        // Half-res ray-march of height fog + sun shafts against the CSM, then composite into the HDR
        // scene (scene·T + inscatter) BEFORE the TAA resolve, so the half-res march noise is cleaned
        // up temporally. Marches against the main pass depth + the (already-current) CsmData UBO.
        if (postProcessingRenderer.VolumetricEnabled && postProcessingRenderer.VolumetricIntensity > 0f)
        {
            _volumetric ??= new VolumetricRenderer(gl);
            _volumetric.Render(
                _framebuffer.DepthTextureHandle, csm.DepthArray, context, invMainViewProj,
                postProcessingRenderer.DensityMultiplier, postProcessingRenderer.ExtinctionMultiplier,
                postProcessingRenderer.VolumetricIntensity, postProcessingRenderer.VolumetricSamples,
                postProcessingRenderer.ScatteringG, ShadowDistance);

            _framebuffer.Bind();
            // Reversed-Z near plane lives in proj M43 (same recovery ShadowCascades uses); needed to
            // linearize depth for the bilateral upsample that suppresses terrain-edge fog halos.
            _volumetric.Composite(context.ScreenWidth, context.ScreenHeight,
                _framebuffer.DepthTextureHandle, context.Projection.M43);
            _framebuffer.Unbind();
        }

        // === TEMPORAL AA RESOLVE (research §9) ===
        // Reproject + blend history into this frame; downstream passes read the resolved HDR.
        var sceneTexture = _framebuffer.TextureHandle;
        if (context.UseTaa)
        {
            sceneTexture = _taa.Resolve(
                _framebuffer.TextureHandle, _framebuffer.DepthTextureHandle,
                _mainViewProj, context.ScreenWidth, context.ScreenHeight);
        }

        // === AUTO-EXPOSURE (research §5.2) ===
        // Histogram the resolved HDR radiance and adapt the exposure before the output transform.
        _autoExposure ??= new AutoExposure(gl);
        _autoExposure.Update(sceneTexture, context.ScreenWidth, context.ScreenHeight,
            context.AutoExposureKey, context.AutoExposureMin, context.AutoExposureMax, context.AutoExposureSpeed);

        // === BLOOM (research §5.6) ===
        // Build the dual-filter pyramid from the resolved HDR scene; composited in the output pass.
        uint bloomTexture = 0;
        var bloomStrength = context.UseBloom ? postProcessingRenderer.BloomIntensity : 0f;
        if (bloomStrength > 0f)
        {
            _bloom ??= new BloomRenderer(gl);
            bloomTexture = _bloom.Render(sceneTexture, context.ScreenWidth, context.ScreenHeight, postProcessingRenderer.BloomThreshold);
        }

        // === OUTPUT TRANSFORM (HDR fp16 → SDR sRGB) ===
        // TAA already resolved spatial aliasing, so skip the FXAA edge filter when it's on.
        _autoExposure.BindForOutput();
        postProcessingRenderer.Gamma = context.Gamma;
        postProcessingRenderer.Render(
            sceneTexture,
            context.ScreenWidth,
            context.ScreenHeight,
            context.IsUnderwater,
            context.Time,
            context.Exposure,
            useFxaa: !context.UseTaa,
            bloomTexture: bloomTexture,
            bloomStrength: bloomStrength);
    }

    private void UpdateUbos(RenderContext context)
    {
        var lightDirection = context.Sun?.Direction ?? Vector3.Normalize(new Vector3(0.8f, -0.5f, 0.1f));
        var lightColor = context.Sun?.Color ?? new Vector3(1.0f, 0.95f, 0.8f);
        var lightIntensity = context.Sun?.Intensity ?? 1.0f;

        // Cascaded shadow matrices: split the view frustum, fit + texel-snap each slice (§8).
        _cascades = ShadowCascades.Compute(
            context.View, context.Projection, lightDirection,
            ShadowDistance, ShadowMapSize, CascadeCount);

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

            ViewPos = new Vector4(context.CameraPosition, 1.0f),
            FogColor = new Vector4(context.FogColor, 1.0f),
            FogNear = context.FogNear,
            FogFar = context.FogFar,
            Exposure = context.Exposure,
            Gamma = context.Gamma,
            View = context.View
        };
        _sceneUbo.Update(sceneData);

        var lightingData = new LightingData
        {
            LightSpaceMatrix = _cascades.LightSpaceMatrices[0], // cascade 0 for water
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

    private static PointLightDataStd140 MapLight(PointLightData light) => new()
    {
        Position = new Vector4(light.Position, 1.0f),
        Color = new Vector4(light.Color, 1.0f),
        Intensity = light.Intensity,
        Constant = light.Constant,
        Linear = light.Linear,
        Quadratic = light.Quadratic
    };

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _sceneUbo.Dispose();
            _lightingUbo.Dispose();
            _csmUbo.Dispose();
            _sunRenderer.Dispose();
            _skyboxRenderer.Dispose();
            cache.Dispose();
            terrainRenderer.Dispose();
            waterRenderer.Dispose();
            torchRenderer.Dispose();
            postProcessingRenderer.Dispose();
            _framebuffer?.Dispose();
            _csm?.Dispose();
            _shadowMapRenderer?.Dispose();
            _iblBaker?.Dispose();
            _clustered?.Dispose();
            _autoExposure?.Dispose();
            _taa?.Dispose();
            _depthPrepass?.Dispose();
            _opaqueColor?.Dispose();
            _gtao?.Dispose();
            _bloom?.Dispose();
            _volumetric?.Dispose();
        }
        _disposed = true;
    }

    private bool _disposed;
}