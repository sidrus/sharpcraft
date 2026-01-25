using SharpCraft.Engine.Rendering.Shaders;

namespace SharpCraft.Engine.Rendering;

public class PostProcessingRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly ShaderProgram _bloomDownsampleShader;
    private readonly ShaderProgram _bloomUpsampleShader;
    private readonly ShaderProgram _volumetricShader;
    private readonly uint _vao;
    private readonly uint _vbo;

    private BloomFramebuffer? _bloomFbo;
    private Framebuffer? _volumetricFbo;
    private int _lastWidth;
    private int _lastHeight;

    // Volumetric scattering parameters
    public float ScatteringG { get; set; } = 0.8f;
    public float DensityMultiplier { get; set; } = 0.02f;
    public float ExtinctionMultiplier { get; set; } = 0.005f;
    public int VolumetricSamples { get; set; } = 48;
    public float VolumetricIntensity { get; set; } = 1.0f;
    
    // Atmosphere scale factors
    public float RayleighScale { get; set; } = 1.0f;
    public float MieScale { get; set; } = 1.0f;
    public float OzoneScale { get; set; } = 1.0f;
    
    // Bloom parameters
    public float BloomIntensity { get; set; } = 0.05f;
    public float BloomThreshold { get; set; } = 1.0f;
    
    // Tone mapping (0=ACES, 1=Filmic, 2=Reinhard)
    public int ToneMapMode { get; set; } = 0;
    
    // Post-processing effects
    public float VignetteIntensity { get; set; } = 0.0f;
    public float ChromaticAberration { get; set; } = 0.0f;

    public PostProcessingRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(_gl, Shaders.Shaders.UnderwaterVertex, Shaders.Shaders.FXAAFragment);
        _bloomDownsampleShader = new ShaderProgram(_gl, Shaders.Shaders.UnderwaterVertex, Shaders.Shaders.BloomDownsampleFragment);
        _bloomUpsampleShader = new ShaderProgram(_gl, Shaders.Shaders.UnderwaterVertex, Shaders.Shaders.BloomUpsampleFragment);
        _volumetricShader = new ShaderProgram(_gl, Shaders.Shaders.UnderwaterVertex, Shaders.Shaders.VolumetricLightingFragment);

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

    public void Render(
        uint textureHandle,
        uint depthTextureHandle,
        uint shadowMapHandle,
        bool isUnderwater,
        float time,
        int width,
        int height,
        System.Numerics.Matrix4x4 invViewProj,
        System.Numerics.Matrix4x4 lightSpaceMatrix,
        System.Numerics.Vector3 lightDir,
        System.Numerics.Vector3 lightColor,
        System.Numerics.Vector3 viewPos)
    {
        if (_bloomFbo == null || _volumetricFbo == null || _lastWidth != width || _lastHeight != height)
        {
            _bloomFbo?.Dispose();
            _volumetricFbo?.Dispose();
            _bloomFbo = new BloomFramebuffer(_gl, width, height);
            _volumetricFbo = new Framebuffer(_gl, width / 2, height / 2, true); // Low-res for performance
            _lastWidth = width;
            _lastHeight = height;
        }

        RenderBloom(textureHandle);
        RenderVolumetric(depthTextureHandle, shadowMapHandle, invViewProj, lightSpaceMatrix, lightDir, lightColor, viewPos);

        _gl.Viewport(0, 0, (uint)width, (uint)height);
        _gl.Disable(EnableCap.DepthTest);
        _shader.Use();
        _shader.SetUniform("screenTexture", 0);
        _shader.SetUniform("bloomTexture", 1);
        _shader.SetUniform("volumetricTexture", 2);
        _shader.SetUniform("bloomIntensity", BloomIntensity);
        _shader.SetUniform("volumetricIntensity", VolumetricIntensity);
        _shader.SetUniform("isUnderwater", isUnderwater ? 1 : 0);
        _shader.SetUniform("time", time);
        _shader.SetUniform("inverseScreenSize", new System.Numerics.Vector2(1.0f / width, 1.0f / height));
        _shader.SetUniform("vignetteIntensity", VignetteIntensity);
        _shader.SetUniform("chromaticAberration", ChromaticAberration);
        _shader.SetUniform("toneMapMode", ToneMapMode);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, textureHandle);
        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, _bloomFbo.Mips[0].Texture);
        _gl.ActiveTexture(TextureUnit.Texture2);
        _gl.BindTexture(TextureTarget.Texture2D, _volumetricFbo.TextureHandle);

        _gl.BindVertexArray(_vao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        _gl.Enable(EnableCap.DepthTest);
    }

    private void RenderVolumetric(
        uint depthTextureHandle,
        uint shadowMapHandle,
        System.Numerics.Matrix4x4 invViewProj,
        System.Numerics.Matrix4x4 lightSpaceMatrix,
        System.Numerics.Vector3 lightDir,
        System.Numerics.Vector3 lightColor,
        System.Numerics.Vector3 viewPos)
    {
        if (_volumetricFbo == null) return;

        _volumetricFbo.Bind();
        _gl.Viewport(0, 0, (uint)_lastWidth / 2, (uint)_lastHeight / 2);
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        _volumetricShader.Use();
        _volumetricShader.SetUniform("depthTexture", 0);
        _volumetricShader.SetUniform("shadowMap", 1);
        _volumetricShader.SetUniform("invViewProj", invViewProj);
        _volumetricShader.SetUniform("lightSpaceMatrix", lightSpaceMatrix);
        _volumetricShader.SetUniform("lightDir", lightDir);
        _volumetricShader.SetUniform("lightColor", lightColor);
        _volumetricShader.SetUniform("viewPos", viewPos);
        
        _volumetricShader.SetUniform("samples", VolumetricSamples);
        _volumetricShader.SetUniform("scatteringG", ScatteringG);
        _volumetricShader.SetUniform("densityMultiplier", DensityMultiplier);
        _volumetricShader.SetUniform("extinctionMultiplier", ExtinctionMultiplier);
        _volumetricShader.SetUniform("rayleighScale", RayleighScale);
        _volumetricShader.SetUniform("mieScale", MieScale);
        _volumetricShader.SetUniform("ozoneScale", OzoneScale);
        
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, depthTextureHandle);
        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, shadowMapHandle);

        _gl.BindVertexArray(_vao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        _volumetricFbo.Unbind();
    }

    private void RenderBloom(uint textureHandle)
    {
        if (_bloomFbo == null) return;

        _bloomFbo.Bind();

        // 1. Downsample
        _bloomDownsampleShader.Use();
        _bloomDownsampleShader.SetUniform("srcTexture", 0);
        _bloomDownsampleShader.SetUniform("bloomThreshold", BloomThreshold);

        uint inputTexture = textureHandle;
        var inputSize = new System.Numerics.Vector2(_lastWidth, _lastHeight);

        for (int i = 0; i < _bloomFbo.Mips.Count; i++)
        {
            var mip = _bloomFbo.Mips[i];
            _gl.Viewport(0, 0, (uint)mip.Size.X, (uint)mip.Size.Y);
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, mip.Texture, 0);

            _bloomDownsampleShader.SetUniform("srcResolution", inputSize);
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, inputTexture);

            _gl.BindVertexArray(_vao);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

            inputSize = mip.Size;
            inputTexture = mip.Texture;
        }

        // 2. Upsample
        _bloomUpsampleShader.Use();
        _bloomUpsampleShader.SetUniform("srcTexture", 0);
        _bloomUpsampleShader.SetUniform("filterRadius", 0.005f);

        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.One, BlendingFactor.One);

        for (int i = _bloomFbo.Mips.Count - 1; i > 0; i--)
        {
            var mip = _bloomFbo.Mips[i];
            var nextMip = _bloomFbo.Mips[i - 1];

            _gl.Viewport(0, 0, (uint)nextMip.Size.X, (uint)nextMip.Size.Y);
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, nextMip.Texture, 0);

            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, mip.Texture);

            _gl.BindVertexArray(_vao);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        _gl.Disable(EnableCap.Blend);
        _bloomFbo.Unbind();
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
                _bloomDownsampleShader.Dispose();
                _bloomUpsampleShader.Dispose();
                _volumetricShader.Dispose();
                _bloomFbo?.Dispose();
                _volumetricFbo?.Dispose();
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
