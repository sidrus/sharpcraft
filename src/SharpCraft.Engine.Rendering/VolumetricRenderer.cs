using SharpCraft.Engine.Rendering.Shaders;
using System.Numerics;

namespace SharpCraft.Engine.Rendering;

/// <summary>
/// Volumetric fog + sun light shafts (research §11 step 10 / §8 "Volumetrics"). A half-resolution
/// ray-march from the camera through a height-fog medium: at each step the cascaded shadow map is
/// sampled so in-scattered sunlight is occluded by geometry, producing crepuscular shafts ("god
/// rays") on top of distance/height haze. The march writes in-scatter (rgb) and Beer-Lambert
/// transmittance (a) into an fp16 target, which is then composited into the HDR scene as
/// <c>scene·T + inscatter</c> via fixed-function blending — done before the TAA resolve so the
/// half-res noise is temporally cleaned up.
/// </summary>
public sealed class VolumetricRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly ShaderProgram _march;
    private readonly ShaderProgram _composite;
    private readonly FullscreenQuad _quad;

    private readonly ColorTarget _target; // rgb = in-scatter, a = transmittance; half-res
    private int _frame;
    private bool _disposed;

    public VolumetricRenderer(GL gl)
    {
        _gl = gl;
        _march = new ShaderProgram(gl, Shaders.Shaders.UnderwaterVertex, Shaders.Shaders.VolumetricMarchFragment);
        _composite = new ShaderProgram(gl, Shaders.Shaders.UnderwaterVertex, Shaders.Shaders.VolumetricCompositeFragment);
        _quad = new FullscreenQuad(gl);
        _target = new ColorTarget(gl, SizedInternalFormat.Rgba16f);
    }

    /// <summary>
    /// Ray-march the fog at half resolution into the scatter target. <paramref name="sceneDepth"/> is
    /// the main pass's reversed-Z depth; <paramref name="shadowArray"/> is the CSM depth array (the
    /// CsmData UBO at binding 2 must already be current). Call after the main forward pass.
    /// </summary>
    public void Render(uint sceneDepth, uint shadowArray, RenderContext context, Matrix4x4 invViewProj,
        float density, float extinction, float intensity, int samples, float mieG, float maxDistance)
    {
        var halfWidth = Math.Max(1, context.Camera.ScreenWidth / 2);
        var halfHeight = Math.Max(1, context.Camera.ScreenHeight / 2);
        _target.EnsureSize(halfWidth, halfHeight);

        var lightDir = context.Lighting.Sun?.Direction ?? Vector3.Normalize(new Vector3(0.8f, -0.5f, 0.1f));
        var toSun = Vector3.Normalize(-lightDir);
        var sunColor = (context.Lighting.Sun?.Color ?? new Vector3(1f, 0.95f, 0.8f)) * (context.Lighting.Sun?.Intensity ?? 0f);

        _target.Bind();
        _gl.Viewport(0, 0, (uint)halfWidth, (uint)halfHeight);
        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.Blend);

        _march.Use();
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, sceneDepth);
        _march.SetUniform("depthTexture", 0);
        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2DArray, shadowArray);
        _march.SetUniform("shadowMap", 1);

        _march.SetUniform("invViewProj", invViewProj);
        _march.SetUniform("cameraPos", context.Camera.CameraPosition);
        _march.SetUniform("sunDirection", toSun);
        _march.SetUniform("sunColor", sunColor);
        _march.SetUniform("fogColor", context.Fog.FogColor);
        _march.SetUniform("density", density);
        _march.SetUniform("extinction", extinction);
        _march.SetUniform("intensity", intensity);
        _march.SetUniform("steps", samples);
        _march.SetUniform("mieG", mieG);
        _march.SetUniform("maxDistance", maxDistance);
        _march.SetUniform("frameJitter", (_frame++ % 8) / 8.0f);

        _quad.Draw();

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Enable(EnableCap.DepthTest);
    }

    /// <summary>
    /// Composite the half-res scatter target into the currently-bound HDR framebuffer:
    /// <c>scene·transmittance + inscatter</c> via blend (ONE, SRC_ALPHA). Bilinear sampling of the
    /// half-res texture is the upsample (fog is low-frequency, so this is sufficient).
    /// </summary>
    public void Composite(int fullWidth, int fullHeight, uint sceneDepth, float near)
    {
        _gl.Viewport(0, 0, (uint)fullWidth, (uint)fullHeight);
        _gl.Disable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.One, BlendingFactor.SrcAlpha); // scene·SRC_ALPHA(=T) + inscatter

        _composite.Use();
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _target.Texture);
        _composite.SetUniform("scatterTexture", 0);
        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, sceneDepth);
        _composite.SetUniform("depthTexture", 1);
        _composite.SetUniform("near", near);

        _quad.Draw();

        _gl.Disable(EnableCap.Blend);
        _gl.Enable(EnableCap.DepthTest);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _march.Dispose();
        _composite.Dispose();
        _quad.Dispose();
        _target.Dispose();
        _disposed = true;
    }
}