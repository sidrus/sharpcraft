using SharpCraft.Engine.Rendering.Shaders;
using System.Numerics;

namespace SharpCraft.Engine.Rendering;

/// <summary>
/// Final output pass: composites the HDR-linear scene to the SDR backbuffer via
/// tonemap → FXAA → sRGB OETF → dither (research §5.3–§5.5, §12.1). Desktop OpenGL has no HDR
/// swapchain, so SDR sRGB is the only display target; the HDR work lives upstream in the fp16
/// scene buffer.
/// </summary>
public class PostProcessingRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly FullscreenQuad _quad;
    private bool _disposed;

    // Tone mapping (0=ACES Hill, 1=AgX, 2=Reinhard) and display-space effects.
    public int ToneMapMode { get; set; } = 0;
    public float VignetteIntensity { get; set; } = 0.0f;
    public float ChromaticAberration { get; set; } = 0.0f;
    public float Gamma { get; set; } = 2.0f;

    // Atmosphere scattering controls — consumed by the skybox via RenderContext.
    public float ScatteringG { get; set; } = 0.8f;
    public float RayleighScale { get; set; } = 1.0f;
    public float MieScale { get; set; } = 1.0f;
    public float OzoneScale { get; set; } = 1.0f;

    // Volumetric fog + sun light shafts (research §11 step 10), consumed by VolumetricRenderer via
    // the pipeline. Density is the scattering/extinction coefficient at the fog floor; extinction is
    // extra haze attenuation; intensity scales the in-scatter; samples is the march step count.
    public bool VolumetricEnabled { get; set; } = true;
    public float DensityMultiplier { get; set; } = 0.012f;
    public float ExtinctionMultiplier { get; set; } = 0.005f;
    public int VolumetricSamples { get; set; } = 48;
    public float VolumetricIntensity { get; set; } = 0.5f; // master fade; subtle by default, push up for haze/shafts
    public float BloomIntensity { get; set; } = 0.05f;
    public float BloomThreshold { get; set; } = 1.0f;

    public PostProcessingRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(_gl, Shaders.Shaders.UnderwaterVertex, Shaders.Shaders.FxaaFragment);
        _quad = new FullscreenQuad(_gl);
    }

    /// <summary>Resolves the HDR scene texture to the SDR backbuffer.</summary>
    public void Render(uint sceneTexture, int width, int height, bool isUnderwater, float time, float manualExposure,
        bool useFxaa = true, uint bloomTexture = 0, float bloomStrength = 0f)
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Viewport(0, 0, (uint)width, (uint)height);
        _gl.Disable(EnableCap.DepthTest);

        _shader.Use();
        _shader.SetUniform("screenTexture", 0);
        _shader.SetUniform("inverseScreenSize", new Vector2(1.0f / width, 1.0f / height));
        _shader.SetUniform("time", time);
        _shader.SetUniform("isUnderwater", isUnderwater ? 1 : 0);
        _shader.SetUniform("vignetteIntensity", VignetteIntensity);
        _shader.SetUniform("chromaticAberration", ChromaticAberration);
        _shader.SetUniform("toneMapMode", ToneMapMode);
        _shader.SetUniform("manualExposure", manualExposure);
        _shader.SetUniform("useFXAA", useFxaa ? 1 : 0);

        _shader.SetUniform("useBloom", bloomTexture > 0 && bloomStrength > 0f ? 1 : 0);
        _shader.SetUniform("bloomStrength", bloomStrength);
        _shader.SetUniform("bloomTexture", 1);
        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, bloomTexture);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, sceneTexture);

        _quad.Draw();

        _gl.Enable(EnableCap.DepthTest);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _shader.Dispose();
        }
        _quad.Dispose();
        _disposed = true;
    }

    ~PostProcessingRenderer()
    {
        Dispose(false);
    }
}