using SharpCraft.Engine.Rendering.Shaders;
using System.Numerics;

namespace SharpCraft.Engine.Rendering;

/// <summary>
/// Screen-space ambient occlusion (research §7). Consumes the depth pre-pass, reconstructs view
/// position/normal, and writes a single-channel AO texture that the forward pass multiplies into
/// its ambient (IBL) term. Noisy per-pixel sampling is denoised temporally by the TAA pass.
/// </summary>
public sealed class GtaoRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly uint _quadVao;
    private readonly uint _quadVbo;

    private uint _fbo;
    private uint _aoTexture;
    private int _width;
    private int _height;
    private int _frame;
    private bool _disposed;

    public uint AoTexture => _aoTexture;

    public GtaoRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(gl, Shaders.Shaders.UnderwaterVertex, Shaders.Shaders.GtaoFragment);

        float[] quad =
        {
            -1f,  1f, 0f, 1f,  -1f, -1f, 0f, 0f,   1f, -1f, 1f, 0f,
            -1f,  1f, 0f, 1f,   1f, -1f, 1f, 0f,   1f,  1f, 1f, 1f
        };
        _quadVao = gl.GenVertexArray();
        _quadVbo = gl.GenBuffer();
        gl.BindVertexArray(_quadVao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVbo);
        unsafe
        {
            fixed (float* p = quad)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(quad.Length * sizeof(float)), p, BufferUsageARB.StaticDraw);
            }

            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
        }
    }

    /// <summary>Compute AO from the pre-pass depth. Returns the AO texture handle.</summary>
    public uint Render(uint depthTexture, Matrix4x4 jitteredProjection, int width, int height, float radius, float intensity)
    {
        EnsureTarget(width, height);
        Matrix4x4.Invert(jitteredProjection, out var invProjection);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        _gl.Viewport(0, 0, (uint)width, (uint)height);
        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.Blend);

        _shader.Use();
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, depthTexture);
        _shader.SetUniform("depthTexture", 0);
        _shader.SetUniform("invProjection", invProjection);
        _shader.SetUniform("projScale", new Vector2(jitteredProjection.M11, jitteredProjection.M22));
        _shader.SetUniform("texelSize", new Vector2(1.0f / width, 1.0f / height));
        _shader.SetUniform("radius", radius);
        _shader.SetUniform("intensity", intensity);
        _shader.SetUniform("frameJitter", (_frame++ % 8) / 8.0f);

        _gl.BindVertexArray(_quadVao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Enable(EnableCap.DepthTest);
        return _aoTexture;
    }

    private void EnsureTarget(int width, int height)
    {
        if (_fbo != 0 && _width == width && _height == height)
        {
            return;
        }

        if (_fbo != 0)
        {
            _gl.DeleteFramebuffer(_fbo);
            _gl.DeleteTexture(_aoTexture);
        }

        _aoTexture = _gl.CreateTexture(TextureTarget.Texture2D);
        _gl.TextureStorage2D(_aoTexture, 1, SizedInternalFormat.R16f, (uint)width, (uint)height);
        _gl.TextureParameter(_aoTexture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TextureParameter(_aoTexture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TextureParameter(_aoTexture, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TextureParameter(_aoTexture, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        _fbo = _gl.CreateFramebuffer();
        _gl.NamedFramebufferTexture(_fbo, FramebufferAttachment.ColorAttachment0, _aoTexture, 0);
        _gl.NamedFramebufferDrawBuffer(_fbo, ColorBuffer.ColorAttachment0);

        _width = width;
        _height = height;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _shader.Dispose();
        _gl.DeleteVertexArray(_quadVao);
        _gl.DeleteBuffer(_quadVbo);
        if (_fbo != 0)
        {
            _gl.DeleteFramebuffer(_fbo);
            _gl.DeleteTexture(_aoTexture);
        }
        _disposed = true;
    }
}