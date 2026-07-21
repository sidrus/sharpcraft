using SharpCraft.Engine.Rendering.Shaders;
using System.Numerics;

namespace SharpCraft.Engine.Rendering.PostProcessing;

/// <summary>
/// Screen-space ambient occlusion (research §7). Consumes the depth pre-pass, reconstructs view
/// position/normal, and writes a single-channel AO texture that the forward pass multiplies into
/// its ambient (IBL) term. Noisy per-pixel sampling is denoised temporally by the TAA pass.
/// </summary>
public sealed class GtaoRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly FullscreenQuad _quad;
    private readonly ColorTarget _target;

    private int _frame;
    private bool _disposed;

    public uint AoTexture => _target.Texture;

    public GtaoRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(gl, Shaders.Shaders.UnderwaterVertex, Shaders.Shaders.GtaoFragment);
        _quad = new FullscreenQuad(gl);
        _target = new ColorTarget(gl, SizedInternalFormat.R16f);
    }

    /// <summary>Compute AO from the pre-pass depth. Returns the AO texture handle.</summary>
    public uint Render(uint depthTexture, Matrix4x4 jitteredProjection, int width, int height, float radius, float intensity)
    {
        _target.EnsureSize(width, height);
        Matrix4x4.Invert(jitteredProjection, out var invProjection);

        _target.Bind();
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

        _quad.Draw();

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Enable(EnableCap.DepthTest);
        return _target.Texture;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _shader.Dispose();
        _quad.Dispose();
        _target.Dispose();
        _disposed = true;
    }
}