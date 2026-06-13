using System.Numerics;
using SharpCraft.Engine.Rendering.Shaders;

namespace SharpCraft.Engine.Rendering.IBL;

/// <summary>
/// Image-based lighting bake (research §4.2/§6). Captures the procedural sky into an environment
/// cubemap, then convolves it into a diffuse irradiance map and a GGX-prefiltered specular map,
/// and bakes the scene-independent DFG/BRDF LUT once. The sky capture + convolutions are
/// re-run only when the sun moves noticeably, so the cost is amortised across many frames.
/// </summary>
public sealed class IblBaker : IDisposable
{
    private const int EnvSize = 128;
    private const int IrradianceSize = 32;
    private const int PrefilterSize = 128;
    private const int PrefilterMips = 5;
    private const int BrdfLutSize = 256;

    private readonly GL _gl;
    private readonly ShaderProgram _skyCapture;
    private readonly ShaderProgram _irradiance;
    private readonly ShaderProgram _prefilter;
    private readonly ShaderProgram _brdfLutShader;

    private readonly uint _envCube;
    private readonly uint _irradianceCube;
    private readonly uint _prefilterCube;
    private readonly uint _brdfLut;
    private readonly uint _fbo;
    private readonly uint _cubeVao;
    private readonly uint _cubeVbo;
    private readonly uint _quadVao;
    private readonly uint _quadVbo;
    private readonly Matrix4x4[] _faceViewProj = new Matrix4x4[6];

    private bool _brdfBaked;
    private Vector3 _lastSunDir = new(float.MaxValue, 0, 0);
    private bool _disposed;

    public uint IrradianceMap => _irradianceCube;
    public uint PrefilterMap => _prefilterCube;
    public uint BrdfLut => _brdfLut;
    public bool IsReady { get; private set; }

    public IblBaker(GL gl)
    {
        _gl = gl;
        _gl.Enable(EnableCap.TextureCubeMapSeamless);

        _skyCapture = new ShaderProgram(gl, Shaders.Shaders.CubemapCaptureVertex, Shaders.Shaders.IblSkyCaptureFragment);
        _irradiance = new ShaderProgram(gl, Shaders.Shaders.CubemapCaptureVertex, Shaders.Shaders.IblIrradianceFragment);
        _prefilter = new ShaderProgram(gl, Shaders.Shaders.CubemapCaptureVertex, Shaders.Shaders.IblPrefilterFragment);
        _brdfLutShader = new ShaderProgram(gl, Shaders.Shaders.UnderwaterVertex, Shaders.Shaders.IblBrdfLutFragment);

        _envCube = CreateCubemap(EnvSize, MipCount(EnvSize), mipmapped: true);
        _irradianceCube = CreateCubemap(IrradianceSize, 1, mipmapped: false);
        _prefilterCube = CreateCubemap(PrefilterSize, PrefilterMips, mipmapped: true);
        _brdfLut = CreateBrdfLutTexture();

        _fbo = _gl.CreateFramebuffer();

        _cubeVao = CreateCube(out _cubeVbo);
        _quadVao = CreateQuad(out _quadVbo);

        BuildFaceMatrices();
    }

    /// <param name="sunDirection">Direction TO the sun (normalized).</param>
    public void Update(Vector3 sunDirection, Vector3 sunColor, float mieG, float captureIntensity)
    {
        if (!_brdfBaked)
        {
            BakeBrdfLut();
            _brdfBaked = true;
        }

        var toSun = Vector3.Normalize(sunDirection);
        if (IsReady && Vector3.Dot(toSun, _lastSunDir) > 0.99985f) return; // ~1° of sun movement
        _lastSunDir = toSun;

        // Bake state: full-screen passes, no depth/cull/blend.
        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.CullFace);
        _gl.Disable(EnableCap.Blend);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

        CaptureSky(toSun, sunColor, mieG, captureIntensity);
        _gl.GenerateTextureMipmap(_envCube);
        ConvolveIrradiance();
        Prefilter();

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Enable(EnableCap.DepthTest);
        IsReady = true;
    }

    private void CaptureSky(Vector3 toSun, Vector3 sunColor, float mieG, float captureIntensity)
    {
        _skyCapture.Use();
        _skyCapture.SetUniform("sunDir", toSun);
        _skyCapture.SetUniform("sunColor", sunColor);
        _skyCapture.SetUniform("mieG", mieG);
        _skyCapture.SetUniform("captureIntensity", captureIntensity);
        RenderCubeFaces(_skyCapture, _envCube, EnvSize, 0);
    }

    private void ConvolveIrradiance()
    {
        _irradiance.Use();
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.TextureCubeMap, _envCube);
        _irradiance.SetUniform("environmentMap", 0);
        RenderCubeFaces(_irradiance, _irradianceCube, IrradianceSize, 0);
    }

    private void Prefilter()
    {
        _prefilter.Use();
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.TextureCubeMap, _envCube);
        _prefilter.SetUniform("environmentMap", 0);
        _prefilter.SetUniform("resolution", (float)EnvSize);

        for (var mip = 0; mip < PrefilterMips; mip++)
        {
            var mipSize = PrefilterSize >> mip;
            var roughness = PrefilterMips > 1 ? (float)mip / (PrefilterMips - 1) : 0f;
            _prefilter.SetUniform("roughness", roughness);
            RenderCubeFaces(_prefilter, _prefilterCube, mipSize, mip);
        }
    }

    private void RenderCubeFaces(ShaderProgram shader, uint cubemap, int size, int mip)
    {
        _gl.Viewport(0, 0, (uint)size, (uint)size);
        _gl.BindVertexArray(_cubeVao);
        for (var face = 0; face < 6; face++)
        {
            shader.SetUniform("viewProjection", _faceViewProj[face]);
            _gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + face,
                cubemap, mip);
            _gl.Clear(ClearBufferMask.ColorBufferBit);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
        }
    }

    private void BakeBrdfLut()
    {
        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.CullFace);
        _gl.Disable(EnableCap.Blend);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _brdfLut, 0);
        _gl.Viewport(0, 0, BrdfLutSize, BrdfLutSize);

        _brdfLutShader.Use();
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        _gl.BindVertexArray(_quadVao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Enable(EnableCap.DepthTest);
    }

    private uint CreateCubemap(int size, int levels, bool mipmapped)
    {
        var tex = _gl.CreateTexture(TextureTarget.TextureCubeMap);
        _gl.TextureStorage2D(tex, (uint)levels, SizedInternalFormat.Rgba16f, (uint)size, (uint)size);
        _gl.TextureParameter(tex, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TextureParameter(tex, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.TextureParameter(tex, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
        _gl.TextureParameter(tex, TextureParameterName.TextureMinFilter,
            (int)(mipmapped ? TextureMinFilter.LinearMipmapLinear : TextureMinFilter.Linear));
        _gl.TextureParameter(tex, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        return tex;
    }

    private uint CreateBrdfLutTexture()
    {
        var tex = _gl.CreateTexture(TextureTarget.Texture2D);
        _gl.TextureStorage2D(tex, 1, SizedInternalFormat.RG16f, BrdfLutSize, BrdfLutSize);
        _gl.TextureParameter(tex, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TextureParameter(tex, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.TextureParameter(tex, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TextureParameter(tex, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        return tex;
    }

    private void BuildFaceMatrices()
    {
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, 1f, 0.1f, 10f);
        // GL cubemap face directions / up vectors (research §12.3 — left-handed face convention).
        (Vector3 target, Vector3 up)[] faces =
        {
            (new Vector3( 1,  0,  0), new Vector3(0, -1,  0)),
            (new Vector3(-1,  0,  0), new Vector3(0, -1,  0)),
            (new Vector3( 0,  1,  0), new Vector3(0,  0,  1)),
            (new Vector3( 0, -1,  0), new Vector3(0,  0, -1)),
            (new Vector3( 0,  0,  1), new Vector3(0, -1,  0)),
            (new Vector3( 0,  0, -1), new Vector3(0, -1,  0)),
        };
        for (var i = 0; i < 6; i++)
        {
            var view = Matrix4x4.CreateLookAt(Vector3.Zero, faces[i].target, faces[i].up);
            _faceViewProj[i] = view * proj; // System.Numerics order, matches the shader convention
        }
    }

    private uint CreateCube(out uint vbo)
    {
        float[] v =
        {
            -1,-1,-1,  1, 1,-1,  1,-1,-1,   1, 1,-1, -1,-1,-1, -1, 1,-1,
            -1,-1, 1,  1,-1, 1,  1, 1, 1,   1, 1, 1, -1, 1, 1, -1,-1, 1,
            -1, 1, 1, -1, 1,-1, -1,-1,-1,  -1,-1,-1, -1,-1, 1, -1, 1, 1,
             1, 1, 1,  1,-1,-1,  1, 1,-1,   1,-1,-1,  1, 1, 1,  1,-1, 1,
            -1,-1,-1,  1,-1,-1,  1,-1, 1,   1,-1, 1, -1,-1, 1, -1,-1,-1,
            -1, 1,-1,  1, 1, 1,  1, 1,-1,   1, 1, 1, -1, 1,-1, -1, 1, 1,
        };
        var vao = _gl.GenVertexArray();
        vbo = _gl.GenBuffer();
        _gl.BindVertexArray(vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        unsafe
        {
            fixed (float* p = v)
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(v.Length * sizeof(float)), p, BufferUsageARB.StaticDraw);
            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
        }
        return vao;
    }

    private uint CreateQuad(out uint vbo)
    {
        float[] v =
        {
            -1f,  1f, 0f, 1f,
            -1f, -1f, 0f, 0f,
             1f, -1f, 1f, 0f,
            -1f,  1f, 0f, 1f,
             1f, -1f, 1f, 0f,
             1f,  1f, 1f, 1f,
        };
        var vao = _gl.GenVertexArray();
        vbo = _gl.GenBuffer();
        _gl.BindVertexArray(vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        unsafe
        {
            fixed (float* p = v)
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(v.Length * sizeof(float)), p, BufferUsageARB.StaticDraw);
            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
        }
        return vao;
    }

    private static int MipCount(int size) => (int)MathF.Floor(MathF.Log2(size)) + 1;

    public void Dispose()
    {
        if (_disposed) return;
        _skyCapture.Dispose();
        _irradiance.Dispose();
        _prefilter.Dispose();
        _brdfLutShader.Dispose();
        _gl.DeleteTexture(_envCube);
        _gl.DeleteTexture(_irradianceCube);
        _gl.DeleteTexture(_prefilterCube);
        _gl.DeleteTexture(_brdfLut);
        _gl.DeleteFramebuffer(_fbo);
        _gl.DeleteVertexArray(_cubeVao);
        _gl.DeleteBuffer(_cubeVbo);
        _gl.DeleteVertexArray(_quadVao);
        _gl.DeleteBuffer(_quadVbo);
        _disposed = true;
    }
}
