using System.Numerics;
using SharpCraft.Engine.Rendering.Shaders;

namespace SharpCraft.Engine.Rendering;

/// <summary>
/// Temporal anti-aliasing (research §9). Jitters the projection sub-pixel each frame (Halton 2,3),
/// then reprojects an accumulated history buffer into the current frame and blends, integrating the
/// jitter into smooth edges. Reprojection is depth-based: the voxel world is fully static, so the
/// previous-frame position of any pixel follows from its depth and the previous view-projection —
/// no per-object motion-vector buffer required. History lives in two ping-ponged fp16 targets.
/// </summary>
public sealed class TemporalAA : IDisposable
{
    private const int JitterSamples = 8;
    private const float BlendFactor = 0.1f; // current-frame weight; history = 0.9

    private readonly GL _gl;
    private readonly ShaderProgram _resolve;
    private readonly uint _quadVao;
    private readonly uint _quadVbo;

    private Framebuffer? _historyA;
    private Framebuffer? _historyB;
    private bool _writeToA = true;       // which target the next resolve writes into
    private Matrix4x4 _prevViewProj = Matrix4x4.Identity;
    private int _frameIndex;
    private bool _historyValid;
    private int _width;
    private int _height;

    public TemporalAA(GL gl)
    {
        _gl = gl;
        _resolve = new ShaderProgram(gl, Shaders.Shaders.UnderwaterVertex, Shaders.Shaders.TaaResolveFragment);

        float[] quad =
        {
            -1f,  1f, 0f, 1f,
            -1f, -1f, 0f, 0f,
             1f, -1f, 1f, 0f,
            -1f,  1f, 0f, 1f,
             1f, -1f, 1f, 0f,
             1f,  1f, 1f, 1f
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
    }

    /// <summary>
    /// Apply this frame's sub-pixel jitter to the projection. Shears clip X/Y by a fraction of a
    /// pixel (reversed-Z safe: only the projection's X/Y are nudged, the depth mapping is untouched).
    /// </summary>
    public Matrix4x4 ApplyJitter(Matrix4x4 projection, int width, int height)
    {
        EnsureTargets(width, height);

        int sample = _frameIndex % JitterSamples + 1; // Halton is 1-based
        float jx = (Halton(sample, 2) - 0.5f) * 2.0f / width;  // pixel offset → NDC
        float jy = (Halton(sample, 3) - 0.5f) * 2.0f / height;

        // Nudge clip X/Y by jitter * w. For the reversed-Z infinite projection (M34 = -1, so
        // clip.w = -viewZ), the w-coupled coefficient of clip.x/y is M31/M32.
        projection.M31 += jx;
        projection.M32 += jy;
        return projection;
    }

    /// <summary>
    /// Reproject + blend history with the current jittered frame. Returns the resolved HDR texture
    /// the downstream passes (auto-exposure, output) should read.
    /// </summary>
    public uint Resolve(uint currentColor, uint depthTexture, Matrix4x4 jitteredViewProj, int width, int height)
    {
        EnsureTargets(width, height);

        var target = _writeToA ? _historyA! : _historyB!;
        var history = _writeToA ? _historyB! : _historyA!;

        Matrix4x4.Invert(jitteredViewProj, out var invViewProj);

        target.Bind();
        _gl.Viewport(0, 0, (uint)width, (uint)height);
        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.Blend);

        _resolve.Use();
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, currentColor);
        _resolve.SetUniform("currentColor", 0);
        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, history.TextureHandle);
        _resolve.SetUniform("historyColor", 1);
        _gl.ActiveTexture(TextureUnit.Texture2);
        _gl.BindTexture(TextureTarget.Texture2D, depthTexture);
        _resolve.SetUniform("depthTexture", 2);

        _resolve.SetUniform("invViewProj", invViewProj);
        _resolve.SetUniform("prevViewProj", _prevViewProj);
        _resolve.SetUniform("texelSize", new Vector2(1.0f / width, 1.0f / height));
        _resolve.SetUniform("blendFactor", BlendFactor);
        _resolve.SetUniform("historyValid", _historyValid ? 1 : 0);

        _gl.BindVertexArray(_quadVao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

        target.Unbind();
        _gl.Enable(EnableCap.DepthTest);

        uint resolved = target.TextureHandle;
        _prevViewProj = jitteredViewProj;
        _writeToA = !_writeToA;
        _frameIndex++;
        _historyValid = true;
        return resolved;
    }

    private void EnsureTargets(int width, int height)
    {
        if (_historyA != null && _width == width && _height == height) return;
        _historyA?.Dispose();
        _historyB?.Dispose();
        _historyA = new Framebuffer(_gl, width, height, hdr: true);
        _historyB = new Framebuffer(_gl, width, height, hdr: true);
        _width = width;
        _height = height;
        _historyValid = false; // history is stale after a resize
    }

    // Halton low-discrepancy sequence — well-distributed sub-pixel sample offsets in [0,1).
    private static float Halton(int index, int radixBase)
    {
        float result = 0f;
        float f = 1f;
        while (index > 0)
        {
            f /= radixBase;
            result += f * (index % radixBase);
            index /= radixBase;
        }
        return result;
    }

    public void Dispose()
    {
        _resolve.Dispose();
        _gl.DeleteVertexArray(_quadVao);
        _gl.DeleteBuffer(_quadVbo);
        _historyA?.Dispose();
        _historyB?.Dispose();
    }
}
