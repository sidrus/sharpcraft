using SharpCraft.Engine.Rendering.Shaders;
using System.Numerics;

namespace SharpCraft.Engine.Rendering;

/// <summary>
/// Dual-filter HDR bloom (research §5.6): the Call of Duty / Jimenez progressive downsample +
/// upsample pyramid. Downsamples the resolved HDR scene through a mip chain (13-tap, Karis average +
/// soft-knee on the first pass), then upsamples back additively with a 3x3 tent. The result (mip 0)
/// is composited into the scene before tonemapping.
/// </summary>
public sealed class BloomRenderer : IDisposable
{
    private const int MaxMips = 6;

    private readonly GL _gl;
    private readonly ShaderProgram _down;
    private readonly ShaderProgram _up;
    private readonly uint _fbo;
    private readonly uint _quadVao;
    private readonly uint _quadVbo;

    private readonly List<(uint tex, int w, int h)> _mips = new();
    private int _srcWidth;
    private int _srcHeight;
    private bool _disposed;

    public BloomRenderer(GL gl)
    {
        _gl = gl;
        _down = new ShaderProgram(gl, Shaders.Shaders.UnderwaterVertex, Shaders.Shaders.BloomDownFragment);
        _up = new ShaderProgram(gl, Shaders.Shaders.UnderwaterVertex, Shaders.Shaders.BloomUpFragment);

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
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(quad.Length * sizeof(float)), p, BufferUsageARB.StaticDraw);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
        }

        _fbo = gl.CreateFramebuffer();
        gl.NamedFramebufferDrawBuffer(_fbo, ColorBuffer.ColorAttachment0);
    }

    /// <summary>Build the bloom pyramid from the HDR scene; returns the bloom texture (mip 0).</summary>
    public uint Render(uint srcHdr, int width, int height, float threshold)
    {
        EnsureMips(width, height);
        if (_mips.Count == 0) return srcHdr;

        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.Blend);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        _gl.BindVertexArray(_quadVao);

        // === Downsample chain ===
        _down.Use();
        for (int i = 0; i < _mips.Count; i++)
        {
            var (tex, w, h) = _mips[i];
            uint srcTex = i == 0 ? srcHdr : _mips[i - 1].tex;
            var srcW = i == 0 ? width : _mips[i - 1].w;
            var srcH = i == 0 ? height : _mips[i - 1].h;

            _gl.NamedFramebufferTexture(_fbo, FramebufferAttachment.ColorAttachment0, tex, 0);
            _gl.Viewport(0, 0, (uint)w, (uint)h);
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, srcTex);
            _down.SetUniform("srcTexture", 0);
            _down.SetUniform("srcTexelSize", new Vector2(1.0f / srcW, 1.0f / srcH));
            _down.SetUniform("firstPass", i == 0 ? 1 : 0);
            _down.SetUniform("threshold", threshold);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        // === Upsample chain (additive accumulation) ===
        _up.Use();
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.One, BlendingFactor.One); // additive; default FuncAdd equation
        for (int i = _mips.Count - 1; i > 0; i--)
        {
            var (srcTex, srcW, srcH) = _mips[i];
            var (dstTex, dstW, dstH) = _mips[i - 1];
            _gl.NamedFramebufferTexture(_fbo, FramebufferAttachment.ColorAttachment0, dstTex, 0);
            _gl.Viewport(0, 0, (uint)dstW, (uint)dstH);
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, srcTex);
            _up.SetUniform("srcTexture", 0);
            _up.SetUniform("srcTexelSize", new Vector2(1.0f / srcW, 1.0f / srcH));
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }
        _gl.Disable(EnableCap.Blend);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Enable(EnableCap.DepthTest);
        return _mips[0].tex;
    }

    private void EnsureMips(int width, int height)
    {
        if (_mips.Count > 0 && _srcWidth == width && _srcHeight == height) return;
        DeleteMips();
        _srcWidth = width;
        _srcHeight = height;

        int w = width, h = height;
        for (int i = 0; i < MaxMips; i++)
        {
            w /= 2; h /= 2;
            if (w < 4 || h < 4) break;

            var tex = _gl.CreateTexture(TextureTarget.Texture2D);
            _gl.TextureStorage2D(tex, 1, SizedInternalFormat.Rgba16f, (uint)w, (uint)h);
            _gl.TextureParameter(tex, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            _gl.TextureParameter(tex, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            _gl.TextureParameter(tex, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            _gl.TextureParameter(tex, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            _mips.Add((tex, w, h));
        }
    }

    private void DeleteMips()
    {
        foreach (var (tex, _, _) in _mips) _gl.DeleteTexture(tex);
        _mips.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _down.Dispose();
        _up.Dispose();
        DeleteMips();
        _gl.DeleteFramebuffer(_fbo);
        _gl.DeleteVertexArray(_quadVao);
        _gl.DeleteBuffer(_quadVbo);
        _disposed = true;
    }
}