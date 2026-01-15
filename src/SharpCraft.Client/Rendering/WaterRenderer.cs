using System.Numerics;
using SharpCraft.Client.Rendering.Shaders;
using SharpCraft.Client.Rendering.Textures;
using SharpCraft.Engine.Universe;
using Silk.NET.OpenGL;

namespace SharpCraft.Client.Rendering;

public class WaterRenderer : IRenderer
{
    private readonly GL _gl;
    private readonly ChunkRenderCache _cache;
    private readonly ChunkMeshManager _meshManager;
    private readonly TextureAtlas _atlas;
    private readonly ShaderProgram _shader;
    private readonly Frustum _frustum = new();
    private readonly uint _vao;

    public WaterRenderer(GL gl, ChunkRenderCache cache, ChunkMeshManager meshManager, TextureAtlas atlas, ShaderProgram shader)
    {
        _gl = gl;
        _cache = cache;
        _meshManager = meshManager;
        _atlas = atlas;
        _vao = gl.GenVertexArray();
        _shader = shader;

        _shader.BindUniformBlock("SceneData", 0);
        _shader.BindUniformBlock("LightingData", 1);
    }

    public void Render(World world, RenderContext context)
    {
        _shader.Use();
        _atlas.Bind(TextureUnit.Texture0, TextureUnit.Texture1, TextureUnit.Texture2, TextureUnit.Texture3);

        _shader.SetUniform("textureAtlas", 0);
        _shader.SetUniform("normalMap", 1);
        _shader.SetUniform("useNormalMap", context.UseNormalMap ? 1 : 0);
        _shader.SetUniform("normalStrength", context.NormalStrength);

        _shader.SetUniform("aoMap", 2);
        _shader.SetUniform("useAO", context.UseAoMap ? 1 : 0);
        _shader.SetUniform("aoMapStrength", context.AoMapStrength);

        _shader.SetUniform("specularMap", 3);
        _shader.SetUniform("useSpecular", context.UseSpecularMap ? 1 : 0);
        _shader.SetUniform("specularMapStrength", context.SpecularMapStrength);

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
                // Shader is shared and managed elsewhere
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