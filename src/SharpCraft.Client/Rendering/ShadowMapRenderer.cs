using System.Numerics;
using SharpCraft.Client.Rendering.Shaders;
using SharpCraft.Engine.Universe;
using Silk.NET.OpenGL;

namespace SharpCraft.Client.Rendering;

public sealed class ShadowMapRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly ChunkRenderCache _cache;
    private readonly ShaderProgram _shader;
    private readonly uint _vao;

    public ShadowMapRenderer(GL gl, ChunkRenderCache cache, ShaderProgram shader)
    {
        _gl = gl;
        _cache = cache;
        _shader = shader;
        _vao = _gl.GenVertexArray();
    }

    public void Render(World world, Matrix4x4 lightSpaceMatrix)
    {
        _shader.Use();
        _shader.SetUniform("lightSpaceMatrix", lightSpaceMatrix);

        _gl.BindVertexArray(_vao);

        foreach (var chunk in world.GetLoadedChunks())
        {
            var chunkPos = chunk.WorldPosition;
            var renderChunk = _cache.Get(chunk);
            if (renderChunk == null) continue;

            var model = Matrix4x4.CreateTranslation(chunkPos);
            _shader.SetUniform("model", model);
            renderChunk.BindAndDrawOpaque();
        }
    }

    public void Dispose()
    {
        _gl.DeleteVertexArray(_vao);
    }
}
