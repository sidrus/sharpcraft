using SharpCraft.Engine.Rendering.Shaders;
using SharpCraft.Engine.Rendering.Textures;
using System.Numerics;

namespace SharpCraft.Engine.Rendering;

public class WaterRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly ChunkRenderCache _cache;
    private readonly ChunkMeshManager _meshManager;
    private readonly TextureAtlas _atlas;
    private readonly ShaderProgram _shader;
    private readonly Frustum _frustum = new();
    private readonly uint _vao;
    private readonly bool _ownsShader;

    public WaterRenderer(GL gl, ChunkRenderCache cache, ChunkMeshManager meshManager, TextureAtlas atlas)
    {
        _gl = gl;
        _cache = cache;
        _meshManager = meshManager;
        _atlas = atlas;
        _vao = gl.GenVertexArray();

        // Create dedicated water shader
        _shader = new ShaderProgram(gl, Shaders.Shaders.WaterVertex, Shaders.Shaders.WaterFragment);
        _ownsShader = true;

        _shader.BindUniformBlock("SceneData", 0);
        _shader.BindUniformBlock("LightingData", 1);
    }

    public void Render(IWorld world, RenderContext context, RenderTargets targets)
    {
        _shader.Use();
        _atlas.Bind();

        _shader.SetUniform("textureAtlas", 0);
        _shader.SetUniform("normalMap", 1);
        _shader.SetUniform("useNormalMap", context.UseNormalMap ? 1 : 0);
        _shader.SetUniform("normalStrength", context.NormalStrength);
        _shader.SetUniform("time", context.Time);

        // Shadow map (cascaded depth array; water samples cascade 0).
        if (targets.ShadowMap > 0)
        {
            _gl.ActiveTexture(TextureUnit.Texture3);
            _gl.BindTexture(TextureTarget.Texture2DArray, targets.ShadowMap);
            _shader.SetUniform("shadowMap", 3);
        }

        // Only enable IBL when all maps are actually available — sampling an unbound
        // cubemap returns black, which would kill the sky reflection entirely.
        var useIbl = context.UseIbl && targets.IrradianceMap != 0 && targets.PrefilterMap != 0 && targets.BrdfLut != 0;
        _shader.SetUniform("useIBL", useIbl ? 1 : 0);
        if (useIbl)
        {
            _gl.ActiveTexture(TextureUnit.Texture6);
            _gl.BindTexture(TextureTarget.TextureCubeMap, targets.IrradianceMap);
            _shader.SetUniform("irradianceMap", 6);

            _gl.ActiveTexture(TextureUnit.Texture7);
            _gl.BindTexture(TextureTarget.TextureCubeMap, targets.PrefilterMap);
            _shader.SetUniform("prefilterMap", 7);

            _gl.ActiveTexture(TextureUnit.Texture8);
            _gl.BindTexture(TextureTarget.Texture2D, targets.BrdfLut);
            _shader.SetUniform("brdfLUT", 8);
        }

        // Screen-space reflections (research §7): ray-march the opaque scene snapshot.
        var useSsr = context.UseSsr && targets.OpaqueColorTexture != 0 && targets.SceneDepthTexture != 0;
        _shader.SetUniform("useSSR", useSsr ? 1 : 0);
        if (useSsr)
        {
            _gl.ActiveTexture(TextureUnit.Texture9);
            _gl.BindTexture(TextureTarget.Texture2D, targets.OpaqueColorTexture);
            _shader.SetUniform("sceneColorTex", 9);
            _gl.ActiveTexture(TextureUnit.Texture10);
            _gl.BindTexture(TextureTarget.Texture2D, targets.SceneDepthTexture);
            _shader.SetUniform("sceneDepthTex", 10);
            _shader.SetUniform("ssrInvViewProj", targets.InvViewProj);
            _shader.SetUniform("invScreenSize", new Vector2(1.0f / context.ScreenWidth, 1.0f / context.ScreenHeight));
        }

        _gl.BindVertexArray(_vao);

        _frustum.Update(context.ViewProjection);

        foreach (var chunk in world.GetLoadedChunks())
        {
            var chunkPos = chunk.WorldPosition;
            if (!_frustum.IsBoxInFrustum(chunkPos, chunkPos + new Vector3(16, 256, 16)))
                continue;

            var renderChunk = _cache.Get(chunk);
            if (chunk.IsDirty) { _meshManager.Enqueue(chunk); }

            var model = Matrix4x4.CreateTranslation(chunkPos);
            _shader.SetUniform("model", model);
            renderChunk.BindAndDrawTransparent();
        }
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
                if (_ownsShader)
                {
                    _shader.Dispose();
                }
            }

            _gl.DeleteVertexArray(_vao);
            _disposed = true;
        }
    }

    ~WaterRenderer()
    {
        Dispose(false);
    }

    private bool _disposed;
}