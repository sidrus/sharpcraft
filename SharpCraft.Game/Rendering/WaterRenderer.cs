using System.Numerics;
using SharpCraft.Core;
using SharpCraft.Game.Rendering.Shaders;
using SharpCraft.Game.Rendering.Textures;
using Silk.NET.OpenGL;

namespace SharpCraft.Game.Rendering;

public class WaterRenderer(GL gl, ChunkRenderCache cache) : IRenderer
{
    private readonly ShaderProgram _shader = new(gl, Shaders.Shaders.DefaultVertex, Shaders.Shaders.DefaultFragment);
    private readonly Texture2d _texture = new(gl, "Assets/Textures/terrain.png");
    private readonly uint _vao = gl.GenVertexArray();

    public void Render(World world, RenderContext context)
    {
        _shader.Use();
        _texture.Bind();
        gl.BindVertexArray(_vao);

        _shader.SetUniform("viewPos", context.CameraPosition);
        _shader.SetUniform("fogColor", context.FogColor);
        _shader.SetUniform("fogNear", context.FogNear);
        _shader.SetUniform("fogFar", context.FogFar);

        _shader.SetUniform("dirLight.direction", Vector3.Normalize(new Vector3(0.5f, -1.0f, 0.3f)));
        _shader.SetUniform("dirLight.color", Vector3.One);
        _shader.SetUniform("exposure", context.Exposure);
        _shader.SetUniform("gamma", context.Gamma);

        foreach (var chunk in world.GetLoadedChunks())
        {
            // Simple distance check or frustum check
            var renderChunk = cache.Get(chunk);

            var model = Matrix4x4.CreateTranslation(chunk.WorldPosition);
            _shader.SetUniform("model", model);
            _shader.SetUniform("mvp", Matrix4x4.CreateTranslation(chunk.WorldPosition) * context.ViewProjection);
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
                _shader.Dispose();
                _texture.Dispose();
                cache.Dispose();
            }

            gl.DeleteVertexArray(_vao);
            _disposed = true;
        }
    }

    private bool _disposed;
}