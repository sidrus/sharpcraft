using SharpCraft.Engine.Rendering.Shaders;

namespace SharpCraft.Engine.Rendering.Post;

/// <summary>
/// Implements automatic exposure adjustment based on scene luminance.
/// Uses luminance histogram or average luminance downsampling.
/// Reference: https://google.github.io/filament/Filament.md.html#imagingpipeline/physicallybasedcamera
/// </summary>
public sealed class AutoExposure : IDisposable
{
    private readonly GL _gl;
    private readonly uint[] _luminanceMips;
    private readonly int _mipLevels;
    private readonly ShaderProgram _luminanceShader;
    private readonly ShaderProgram _adaptShader;
    private readonly uint _quadVao;
    private readonly uint _quadVbo;
    private readonly uint _luminanceFbo;

    private uint _currentLuminanceTexture;
    private uint _previousLuminanceTexture;
    private float _currentExposure = 1.0f;
    private bool _disposed;

    /// <summary>
    /// Current calculated exposure value.
    /// </summary>
    public float Exposure => _currentExposure;

    /// <summary>
    /// Minimum exposure value.
    /// </summary>
    public float MinExposure { get; set; } = 0.1f;

    /// <summary>
    /// Maximum exposure value.
    /// </summary>
    public float MaxExposure { get; set; } = 10.0f;

    /// <summary>
    /// Speed of exposure adaptation (higher = faster).
    /// </summary>
    public float AdaptationSpeed { get; set; } = 1.5f;

    /// <summary>
    /// Target middle gray value for exposure calculation.
    /// </summary>
    public float KeyValue { get; set; } = 0.18f;

    /// <summary>
    /// Manual exposure compensation in EV stops.
    /// </summary>
    public float ExposureCompensation { get; set; } = 0.0f;

    public AutoExposure(GL gl, int initialWidth, int initialHeight)
    {
        _gl = gl;

        // Calculate mip levels for luminance downsampling
        var size = Math.Max(initialWidth, initialHeight);
        _mipLevels = (int)Math.Floor(Math.Log2(size)) + 1;

        // Create luminance textures for ping-pong
        _luminanceMips = new uint[2];
        for (var i = 0; i < 2; i++)
        {
            _luminanceMips[i] = CreateLuminanceTexture(initialWidth, initialHeight);
        }
        _currentLuminanceTexture = _luminanceMips[0];
        _previousLuminanceTexture = _luminanceMips[1];

        _luminanceFbo = _gl.GenFramebuffer();

        // Create shaders
        _luminanceShader = new ShaderProgram(gl, LuminanceShaders.Vertex, LuminanceShaders.Fragment);
        _adaptShader = new ShaderProgram(gl, LuminanceShaders.Vertex, LuminanceShaders.AdaptFragment);

        // Create fullscreen quad
        (_quadVao, _quadVbo) = CreateQuadMesh();
    }

    /// <summary>
    /// Calculates the average luminance of the scene and updates exposure.
    /// </summary>
    /// <param name="hdrTexture">HDR scene texture</param>
    /// <param name="deltaTime">Frame delta time for smooth adaptation</param>
    public void Update(uint hdrTexture, float deltaTime)
    {
        // Swap luminance textures
        (_currentLuminanceTexture, _previousLuminanceTexture) = (_previousLuminanceTexture, _currentLuminanceTexture);

        // Calculate luminance and downsample to 1x1
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _luminanceFbo);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, _currentLuminanceTexture, 0);

        _luminanceShader.Use();
        _luminanceShader.SetUniform("hdrTexture", 0);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, hdrTexture);

        _gl.Viewport(0, 0, 1, 1);
        _gl.Disable(EnableCap.DepthTest);

        _gl.BindVertexArray(_quadVao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

        // Read back average luminance
        Span<float> luminanceData = stackalloc float[4];
        _gl.ReadPixels(0, 0, 1, 1, PixelFormat.Rgba, PixelType.Float, luminanceData);
        var avgLuminance = Math.Max(luminanceData[0], 0.001f);

        // Calculate target exposure using Reinhard's key value formula
        // exposure = keyValue / avgLuminance
        var targetExposure = KeyValue / avgLuminance;

        // Apply exposure compensation (in EV stops)
        targetExposure *= MathF.Pow(2.0f, ExposureCompensation);

        // Clamp to valid range
        targetExposure = Math.Clamp(targetExposure, MinExposure, MaxExposure);

        // Smooth adaptation
        var adaptSpeed = AdaptationSpeed * deltaTime;
        if (targetExposure > _currentExposure)
        {
            // Adapting to brighter scene (slower)
            _currentExposure = MathHelper.Lerp(_currentExposure, targetExposure, adaptSpeed * 0.5f);
        }
        else
        {
            // Adapting to darker scene (faster)
            _currentExposure = MathHelper.Lerp(_currentExposure, targetExposure, adaptSpeed);
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Enable(EnableCap.DepthTest);
    }

    /// <summary>
    /// Resets exposure to a specific value (useful for scene transitions).
    /// </summary>
    public void Reset(float exposure = 1.0f)
    {
        _currentExposure = Math.Clamp(exposure, MinExposure, MaxExposure);
    }

    private uint CreateLuminanceTexture(int width, int height)
    {
        var texture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, texture);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.R16f, 1, 1, 0,
            PixelFormat.Red, PixelType.Float, ReadOnlySpan<byte>.Empty);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        return texture;
    }

    private (uint vao, uint vbo) CreateQuadMesh()
    {
        float[] vertices =
        [
            -1.0f,  1.0f,  0.0f, 1.0f,
            -1.0f, -1.0f,  0.0f, 0.0f,
             1.0f, -1.0f,  1.0f, 0.0f,
            -1.0f,  1.0f,  0.0f, 1.0f,
             1.0f, -1.0f,  1.0f, 0.0f,
             1.0f,  1.0f,  1.0f, 1.0f
        ];

        var vao = _gl.GenVertexArray();
        var vbo = _gl.GenBuffer();

        _gl.BindVertexArray(vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        unsafe
        {
            fixed (float* v = vertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }
        }

        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

        _gl.EnableVertexAttribArray(1);
        unsafe
        {
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
        }

        _gl.BindVertexArray(0);

        return (vao, vbo);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _luminanceShader.Dispose();
        _adaptShader.Dispose();

        foreach (var tex in _luminanceMips)
        {
            _gl.DeleteTexture(tex);
        }

        _gl.DeleteFramebuffer(_luminanceFbo);
        _gl.DeleteVertexArray(_quadVao);
        _gl.DeleteBuffer(_quadVbo);

        _disposed = true;
    }
}

internal static class MathHelper
{
    public static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0f, 1f);
}

internal static class LuminanceShaders
{
    public const string Vertex = """
        #version 450 core
        layout (location = 0) in vec2 aPos;
        layout (location = 1) in vec2 aTexCoords;

        out vec2 TexCoords;

        void main()
        {
            TexCoords = aTexCoords;
            gl_Position = vec4(aPos, 0.0, 1.0);
        }
        """;

    /// <summary>
    /// Calculates log-average luminance of the scene.
    /// Uses a weighted average that samples the entire texture.
    /// </summary>
    public const string Fragment = """
        #version 450 core
        out vec4 FragColor;
        in vec2 TexCoords;

        uniform sampler2D hdrTexture;

        // Luminance weights (Rec. 709)
        const vec3 LUMINANCE_WEIGHTS = vec3(0.2126, 0.7152, 0.0722);

        void main()
        {
            // Sample multiple points across the texture for average
            vec2 texelSize = 1.0 / textureSize(hdrTexture, 0);
            
            float totalLuminance = 0.0;
            float sampleCount = 0.0;
            
            // Sample a grid across the entire image
            const int SAMPLES = 16;
            for (int y = 0; y < SAMPLES; y++)
            {
                for (int x = 0; x < SAMPLES; x++)
                {
                    vec2 uv = vec2(float(x) + 0.5, float(y) + 0.5) / float(SAMPLES);
                    vec3 color = texture(hdrTexture, uv).rgb;
                    float luminance = dot(color, LUMINANCE_WEIGHTS);
                    
                    // Use log-average for better handling of high dynamic range
                    totalLuminance += log(max(luminance, 0.0001));
                    sampleCount += 1.0;
                }
            }
            
            // Convert back from log space
            float avgLuminance = exp(totalLuminance / sampleCount);
            
            FragColor = vec4(avgLuminance, 0.0, 0.0, 1.0);
        }
        """;

    public const string AdaptFragment = """
        #version 450 core
        out vec4 FragColor;
        in vec2 TexCoords;

        uniform sampler2D currentLuminance;
        uniform sampler2D previousLuminance;
        uniform float adaptationSpeed;
        uniform float deltaTime;

        void main()
        {
            float current = texture(currentLuminance, vec2(0.5)).r;
            float previous = texture(previousLuminance, vec2(0.5)).r;
            
            float adapted = previous + (current - previous) * (1.0 - exp(-deltaTime * adaptationSpeed));
            
            FragColor = vec4(adapted, 0.0, 0.0, 1.0);
        }
        """;
}
