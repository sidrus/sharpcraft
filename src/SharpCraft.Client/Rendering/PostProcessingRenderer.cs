using SharpCraft.Client.Rendering.Shaders;
using Silk.NET.OpenGL;

namespace SharpCraft.Client.Rendering;

public class PostProcessingRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly uint _vao;
    private readonly uint _vbo;

    public PostProcessingRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(_gl, Shaders.Shaders.UnderwaterVertex, Shaders.Shaders.FXAAFragment);

        float[] quadVertices = {
            // positions   // texCoords
            -1.0f,  1.0f,  0.0f, 1.0f,
            -1.0f, -1.0f,  0.0f, 0.0f,
             1.0f, -1.0f,  1.0f, 0.0f,

            -1.0f,  1.0f,  0.0f, 1.0f,
             1.0f, -1.0f,  1.0f, 0.0f,
             1.0f,  1.0f,  1.0f, 1.0f
        };

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        unsafe
        {
            fixed (float* v = quadVertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(quadVertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }
        }

        _gl.EnableVertexAttribArray(0);
        unsafe
        {
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
        }
    }

    public void Render(uint textureHandle, bool isUnderwater, float time, int width, int height)
    {
        _gl.Disable(EnableCap.DepthTest);
        _shader.Use();
        _shader.SetUniform("screenTexture", 0);
        _shader.SetUniform("isUnderwater", isUnderwater ? 1 : 0);
        _shader.SetUniform("time", time);
        _shader.SetUniform("inverseScreenSize", new System.Numerics.Vector2(1.0f / width, 1.0f / height));

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, textureHandle);

        _gl.BindVertexArray(_vao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        _gl.Enable(EnableCap.DepthTest);
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
            }

            _gl.DeleteVertexArray(_vao);
            _gl.DeleteBuffer(_vbo);
            _disposed = true;
        }
    }

    ~PostProcessingRenderer()
    {
        Dispose(false);
    }

    private bool _disposed;
}
