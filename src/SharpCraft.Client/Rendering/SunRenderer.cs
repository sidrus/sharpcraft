using System.Numerics;
using SharpCraft.Client.Rendering.Shaders;
using Silk.NET.OpenGL;

namespace SharpCraft.Client.Rendering;

public sealed class SunRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly uint _vao;
    private readonly uint _vbo;

    public SunRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(_gl, Shaders.Shaders.SunVertex, Shaders.Shaders.SunFragment);
        
        _shader.BindUniformBlock("SceneData", 0);

        float[] vertices = {
            -1.0f,  1.0f, 0.0f,
            -1.0f, -1.0f, 0.0f,
             1.0f, -1.0f, 0.0f,
            -1.0f,  1.0f, 0.0f,
             1.0f, -1.0f, 0.0f,
             1.0f,  1.0f, 0.0f
        };

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        unsafe
        {
            fixed (float* v = vertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }
        }

        _gl.EnableVertexAttribArray(0);
        unsafe
        {
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
        }
        
        _gl.BindVertexArray(0);
    }

    public void Render(RenderContext context)
    {
        if (context.Sun == null || context.Sun.Value.Intensity <= 0) return;

        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.Disable(EnableCap.CullFace);
        _gl.DepthFunc(DepthFunction.Lequal);

        _shader.Use();
        
        var sunDir = Vector3.Normalize(-context.Sun.Value.Direction);
        _shader.SetUniform("sunDir", sunDir);
        _shader.SetUniform("sunColor", context.Sun.Value.Color);
        _shader.SetUniform("sunIntensity", context.Sun.Value.Intensity);
        _shader.SetUniform("sunSize", 2.0f); // Reduced from 10.0f

        _gl.BindVertexArray(_vao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

        _gl.DepthFunc(DepthFunction.Less);
        _gl.Enable(EnableCap.CullFace);
        _gl.Disable(EnableCap.Blend);
    }

    public void Dispose()
    {
        _shader.Dispose();
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
    }
}
