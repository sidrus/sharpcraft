using System.Numerics;
using SharpCraft.Engine.Rendering.Shaders;
using SharpCraft.Engine.Rendering.Textures;

namespace SharpCraft.Engine.Rendering;

public class WaterRenderer : IRenderer
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

    public void Render(IWorld world, RenderContext context)
    {
        _shader.Use();
        _atlas.Bind(
            TextureUnit.Texture0, 
            TextureUnit.Texture1, 
            TextureUnit.Texture2, 
            TextureUnit.Texture3,
            TextureUnit.Texture4,
            TextureUnit.Texture5);

        _shader.SetUniform("textureAtlas", 0);
        _shader.SetUniform("normalMap", 1);
        _shader.SetUniform("useNormalMap", context.UseNormalMap ? 1 : 0);
        _shader.SetUniform("normalStrength", context.NormalStrength);
        _shader.SetUniform("time", context.Time);

        // Shadow map
        if (context.ShadowMap > 0)
        {
            _gl.ActiveTexture(TextureUnit.Texture3);
            _gl.BindTexture(TextureTarget.Texture2D, context.ShadowMap);
            _shader.SetUniform("shadowMap", 3);
        }

        _shader.SetUniform("useIBL", context.UseIBL ? 1 : 0);
        if (context.UseIBL)
        {
            _gl.ActiveTexture(TextureUnit.Texture6);
            _gl.BindTexture(TextureTarget.TextureCubeMap, context.IrradianceMap);
            _shader.SetUniform("irradianceMap", 6);

            _gl.ActiveTexture(TextureUnit.Texture7);
            _gl.BindTexture(TextureTarget.TextureCubeMap, context.PrefilterMap);
            _shader.SetUniform("prefilterMap", 7);

            _gl.ActiveTexture(TextureUnit.Texture8);
            _gl.BindTexture(TextureTarget.Texture2D, context.BrdfLut);
            _shader.SetUniform("brdfLUT", 8);
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