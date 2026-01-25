using System.Numerics;
using SharpCraft.Client.Rendering.Shaders;
using Silk.NET.OpenGL;

namespace SharpCraft.Client.Rendering.IBL;

/// <summary>
/// Generates Image-Based Lighting (IBL) resources for PBR rendering.
/// Implements the split-sum approximation used in UE4/5 and other modern engines.
/// Reference: https://google.github.io/filament/Filament.md.html#lighting/imagebasedlights
/// </summary>
public sealed class IBLGenerator : IDisposable
{
    private readonly GL _gl;
    private readonly uint _captureFbo;
    private readonly uint _captureRbo;
    private readonly uint _cubeVao;
    private readonly uint _cubeVbo;
    private readonly uint _quadVao;
    private readonly uint _quadVbo;

    private ShaderProgram? _equirectToCubemapShader;
    private ShaderProgram? _irradianceShader;
    private ShaderProgram? _prefilterShader;
    private ShaderProgram? _brdfShader;

    private bool _disposed;

    // Capture projection and view matrices for rendering to cubemap faces
    private static readonly Matrix4x4 CaptureProjection = Matrix4x4.CreatePerspectiveFieldOfView(
        MathF.PI / 2f, 1.0f, 0.1f, 10.0f);

    private static readonly Matrix4x4[] CaptureViews =
    [
        Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitX, -Vector3.UnitY),   // +X
        Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitX, -Vector3.UnitY),  // -X
        Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitY, Vector3.UnitZ),    // +Y
        Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitY, -Vector3.UnitZ),  // -Y
        Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitZ, -Vector3.UnitY),   // +Z
        Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitZ, -Vector3.UnitY)   // -Z
    ];

    public IBLGenerator(GL gl)
    {
        _gl = gl;

        // Create capture framebuffer
        _captureFbo = _gl.GenFramebuffer();
        _captureRbo = _gl.GenRenderbuffer();

        // Create cube mesh for rendering to cubemap
        (_cubeVao, _cubeVbo) = CreateCubeMesh();

        // Create quad mesh for BRDF LUT generation
        (_quadVao, _quadVbo) = CreateQuadMesh();
    }

    /// <summary>
    /// Converts an equirectangular HDR environment map to a cubemap.
    /// </summary>
    /// <param name="equirectTexture">Source equirectangular texture handle</param>
    /// <param name="cubemapSize">Size of each cubemap face</param>
    /// <returns>Generated cubemap</returns>
    public Cubemap GenerateEnvironmentCubemap(uint equirectTexture, int cubemapSize = 512)
    {
        EnsureEquirectShader();

        var envCubemap = new Cubemap(_gl, cubemapSize, InternalFormat.Rgba16f, true);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _captureFbo);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _captureRbo);
        _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24,
            (uint)cubemapSize, (uint)cubemapSize);
        _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer, _captureRbo);

        _equirectToCubemapShader!.Use();
        _equirectToCubemapShader.SetUniform("equirectangularMap", 0);
        _equirectToCubemapShader.SetUniform("projection", CaptureProjection);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, equirectTexture);

        _gl.Viewport(0, 0, (uint)cubemapSize, (uint)cubemapSize);

        for (var i = 0; i < 6; i++)
        {
            _equirectToCubemapShader.SetUniform("view", CaptureViews[i]);
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + i, envCubemap.Handle, 0);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            RenderCube();
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // Generate mipmaps for the environment cubemap (used for roughness levels in prefiltering)
        envCubemap.GenerateMipmaps();

        return envCubemap;
    }

    /// <summary>
    /// Generates an irradiance cubemap for diffuse IBL.
    /// Convolves the environment map to capture diffuse irradiance from all directions.
    /// </summary>
    /// <param name="envCubemap">Source environment cubemap</param>
    /// <param name="irradianceSize">Size of irradiance cubemap (typically 32)</param>
    /// <returns>Generated irradiance cubemap</returns>
    public Cubemap GenerateIrradianceMap(Cubemap envCubemap, int irradianceSize = 32)
    {
        EnsureIrradianceShader();

        var irradianceMap = new Cubemap(_gl, irradianceSize, InternalFormat.Rgba16f, false);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _captureFbo);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _captureRbo);
        _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24,
            (uint)irradianceSize, (uint)irradianceSize);

        _irradianceShader!.Use();
        _irradianceShader.SetUniform("environmentMap", 0);
        _irradianceShader.SetUniform("projection", CaptureProjection);

        envCubemap.Bind(0);

        _gl.Viewport(0, 0, (uint)irradianceSize, (uint)irradianceSize);

        for (var i = 0; i < 6; i++)
        {
            _irradianceShader.SetUniform("view", CaptureViews[i]);
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + i, irradianceMap.Handle, 0);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            RenderCube();
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        return irradianceMap;
    }

    /// <summary>
    /// Generates a prefiltered environment map for specular IBL.
    /// Each mip level stores the environment convolved with increasing roughness.
    /// Uses importance sampling of the GGX distribution.
    /// Reference: https://learnopengl.com/PBR/IBL/Specular-IBL
    /// </summary>
    /// <param name="envCubemap">Source environment cubemap</param>
    /// <param name="prefilterSize">Size of prefiltered cubemap (typically 128)</param>
    /// <returns>Generated prefiltered cubemap with roughness in mip levels</returns>
    public Cubemap GeneratePrefilterMap(Cubemap envCubemap, int prefilterSize = 128)
    {
        EnsurePrefilterShader();

        var prefilterMap = new Cubemap(_gl, prefilterSize, InternalFormat.Rgba16f, true);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _captureFbo);

        _prefilterShader!.Use();
        _prefilterShader.SetUniform("environmentMap", 0);
        _prefilterShader.SetUniform("projection", CaptureProjection);
        _prefilterShader.SetUniform("envResolution", (float)envCubemap.Size);

        envCubemap.Bind(0);

        var maxMipLevels = 5; // 5 roughness levels: 0.0, 0.25, 0.5, 0.75, 1.0

        for (var mip = 0; mip < maxMipLevels; mip++)
        {
            var mipSize = (uint)(prefilterSize * MathF.Pow(0.5f, mip));
            var roughness = (float)mip / (maxMipLevels - 1);

            _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _captureRbo);
            _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, mipSize, mipSize);

            _gl.Viewport(0, 0, mipSize, mipSize);
            _prefilterShader.SetUniform("roughness", roughness);

            for (var i = 0; i < 6; i++)
            {
                _prefilterShader.SetUniform("view", CaptureViews[i]);
                _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                    TextureTarget.TextureCubeMapPositiveX + i, prefilterMap.Handle, mip);
                _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                RenderCube();
            }
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        return prefilterMap;
    }

    /// <summary>
    /// Generates the BRDF integration LUT for the split-sum approximation.
    /// Stores the scale and bias for the Fresnel term based on roughness and NdotV.
    /// This is view-independent and only needs to be generated once.
    /// </summary>
    /// <param name="lutSize">Size of the LUT texture (typically 512)</param>
    /// <returns>BRDF LUT texture handle</returns>
    public uint GenerateBrdfLut(int lutSize = 512)
    {
        EnsureBrdfShader();

        var brdfLut = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, brdfLut);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.RG16f,
            (uint)lutSize, (uint)lutSize, 0, PixelFormat.RG, PixelType.Float, ReadOnlySpan<byte>.Empty);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _captureFbo);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _captureRbo);
        _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24,
            (uint)lutSize, (uint)lutSize);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, brdfLut, 0);

        _gl.Viewport(0, 0, (uint)lutSize, (uint)lutSize);
        _brdfShader!.Use();
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        RenderQuad();

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        return brdfLut;
    }

    private void EnsureEquirectShader()
    {
        _equirectToCubemapShader ??= new ShaderProgram(_gl,
            IBLShaders.EquirectToCubemapVertex,
            IBLShaders.EquirectToCubemapFragment);
    }

    private void EnsureIrradianceShader()
    {
        _irradianceShader ??= new ShaderProgram(_gl,
            IBLShaders.IrradianceVertex,
            IBLShaders.IrradianceFragment);
    }

    private void EnsurePrefilterShader()
    {
        _prefilterShader ??= new ShaderProgram(_gl,
            IBLShaders.PrefilterVertex,
            IBLShaders.PrefilterFragment);
    }

    private void EnsureBrdfShader()
    {
        _brdfShader ??= new ShaderProgram(_gl,
            IBLShaders.BrdfVertex,
            IBLShaders.BrdfFragment);
    }

    private (uint vao, uint vbo) CreateCubeMesh()
    {
        float[] vertices =
        [
            // Back face
            -1.0f, -1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            // Front face
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,
            // Left face
            -1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
            // Right face
             1.0f,  1.0f,  1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f, -1.0f,  1.0f,
            // Bottom face
            -1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f,  1.0f,
             1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f, -1.0f,
            // Top face
            -1.0f,  1.0f, -1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f,  1.0f
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
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

        _gl.BindVertexArray(0);

        return (vao, vbo);
    }

    private (uint vao, uint vbo) CreateQuadMesh()
    {
        float[] vertices =
        [
            // positions   // texCoords
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

    private void RenderCube()
    {
        _gl.BindVertexArray(_cubeVao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
        _gl.BindVertexArray(0);
    }

    private void RenderQuad()
    {
        _gl.BindVertexArray(_quadVao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        _gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _equirectToCubemapShader?.Dispose();
        _irradianceShader?.Dispose();
        _prefilterShader?.Dispose();
        _brdfShader?.Dispose();

        _gl.DeleteFramebuffer(_captureFbo);
        _gl.DeleteRenderbuffer(_captureRbo);
        _gl.DeleteVertexArray(_cubeVao);
        _gl.DeleteBuffer(_cubeVbo);
        _gl.DeleteVertexArray(_quadVao);
        _gl.DeleteBuffer(_quadVbo);

        _disposed = true;
    }

    ~IBLGenerator()
    {
        // Note: OpenGL resources should be deleted on the GL thread
    }
}
