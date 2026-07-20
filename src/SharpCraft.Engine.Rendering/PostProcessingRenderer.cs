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
    private readonly uint _vao;
    private readonly uint _vbo;
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

        float[] quadVertices =
        {
            -1.0f,  1.0f, 0.0f, 1.0f,
            -1.0f, -1.0f, 0.0f, 0.0f,
             1.0f, -1.0f, 1.0f, 0.0f,
            -1.0f,  1.0f, 0.0f, 1.0f,
             1.0f, -1.0f, 1.0f, 0.0f,
             1.0f,  1.0f, 1.0f, 1.0f
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
            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
        }
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
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _shader.Dispose();
        }
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _disposed = true;
    }

    ~PostProcessingRenderer()
    {
        Dispose(false);
    }
}