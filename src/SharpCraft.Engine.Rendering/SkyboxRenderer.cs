using System.Numerics;
using SharpCraft.Engine.Rendering.Shaders;

namespace SharpCraft.Engine.Rendering;

public sealed class SkyboxRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly uint _vao;
    private readonly uint _vbo;

    public SkyboxRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(_gl, Shaders.Shaders.SkyboxVertex, Shaders.Shaders.SkyboxFragment);
        _shader.BindUniformBlock("SceneData", 0);

        float[] vertices = {
            // Positions          
            -1.0f,  1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,

            -1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,

             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,

            -1.0f, -1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,

            -1.0f,  1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f, -1.0f,

            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f,  1.0f
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
        _gl.DepthFunc(DepthFunction.Lequal);
        _gl.Disable(EnableCap.CullFace);

        _shader.Use();
        
        // Pass Sun and Moon data
        var sunDir = context.Sun?.Direction ?? new Vector3(0, -1, 0);
        var sunColor = context.Sun?.Color ?? Vector3.One;
        var sunIntensity = context.Sun?.Intensity ?? 0f;
        
        // Simple Moon (opposite to Sun)
        var moonDir = -sunDir;
        var moonColor = new Vector3(0.5f, 0.6f, 1.0f);
        var moonIntensity = 1.0f - sunIntensity;
        
        _shader.SetUniform("sunDir", sunDir);
        _shader.SetUniform("sunColor", sunColor);
        _shader.SetUniform("sunIntensity", sunIntensity);
        
        _shader.SetUniform("moonDir", moonDir);
        _shader.SetUniform("moonColor", moonColor);
        _shader.SetUniform("moonIntensity", moonIntensity);
        
        // Atmosphere parameters from UI
        _shader.SetUniform("atmosphereRayleighScale", context.AtmosphereRayleighScale);
        _shader.SetUniform("atmosphereMieScale", context.AtmosphereMieScale);
        _shader.SetUniform("atmosphereOzoneScale", context.AtmosphereOzoneScale);
        _shader.SetUniform("atmosphereMieG", context.AtmosphereMieG);

        _gl.BindVertexArray(_vao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

        _gl.Enable(EnableCap.CullFace);
        _gl.DepthFunc(DepthFunction.Less);
    }

    public void Dispose()
    {
        _shader.Dispose();
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
    }
}
