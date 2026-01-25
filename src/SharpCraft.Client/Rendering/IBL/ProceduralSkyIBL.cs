using System.Numerics;
using SharpCraft.Client.Rendering.Shaders;
using Silk.NET.OpenGL;

namespace SharpCraft.Client.Rendering.IBL;

/// <summary>
/// Generates IBL cubemaps from the procedural atmosphere model.
/// This allows PBR materials to receive proper ambient lighting from the sky
/// without requiring an external HDRI.
/// </summary>
public sealed class ProceduralSkyIBL : IDisposable
{
    private readonly GL _gl;
    private readonly uint _captureFbo;
    private readonly uint _captureRbo;
    private readonly uint _cubeVao;
    private readonly uint _cubeVbo;
    private ShaderProgram? _skyToCubemapShader;
    private bool _disposed;

    private static readonly Matrix4x4 CaptureProjection = Matrix4x4.CreatePerspectiveFieldOfView(
        MathF.PI / 2f, 1.0f, 0.1f, 10.0f);

    private static readonly Matrix4x4[] CaptureViews =
    [
        Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitX, -Vector3.UnitY),
        Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitX, -Vector3.UnitY),
        Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitY, Vector3.UnitZ),
        Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitY, -Vector3.UnitZ),
        Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitZ, -Vector3.UnitY),
        Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitZ, -Vector3.UnitY)
    ];

    public ProceduralSkyIBL(GL gl)
    {
        _gl = gl;
        _captureFbo = _gl.GenFramebuffer();
        _captureRbo = _gl.GenRenderbuffer();
        (_cubeVao, _cubeVbo) = CreateCubeMesh();
    }

    /// <summary>
    /// Generates a sky cubemap from the atmosphere model for the given sun direction.
    /// </summary>
    /// <param name="sunDirection">Normalized sun direction vector</param>
    /// <param name="sunIntensity">Sun light intensity</param>
    /// <param name="cubemapSize">Size of each cubemap face</param>
    /// <returns>Generated sky cubemap</returns>
    public Cubemap GenerateSkyCubemap(Vector3 sunDirection, float sunIntensity = 20.0f, int cubemapSize = 256)
    {
        EnsureShader();

        var skyCubemap = new Cubemap(_gl, cubemapSize, InternalFormat.Rgba16f, true);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _captureFbo);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _captureRbo);
        _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24,
            (uint)cubemapSize, (uint)cubemapSize);
        _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer, _captureRbo);

        _skyToCubemapShader!.Use();
        _skyToCubemapShader.SetUniform("projection", CaptureProjection);
        _skyToCubemapShader.SetUniform("sunDirection", sunDirection);
        _skyToCubemapShader.SetUniform("sunIntensity", sunIntensity);

        _gl.Viewport(0, 0, (uint)cubemapSize, (uint)cubemapSize);
        _gl.Disable(EnableCap.DepthTest);

        for (var i = 0; i < 6; i++)
        {
            _skyToCubemapShader.SetUniform("view", CaptureViews[i]);
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + i, skyCubemap.Handle, 0);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            RenderCube();
        }

        _gl.Enable(EnableCap.DepthTest);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        skyCubemap.GenerateMipmaps();

        return skyCubemap;
    }

    private void EnsureShader()
    {
        _skyToCubemapShader ??= new ShaderProgram(_gl,
            ProceduralSkyShaders.SkyToCubemapVertex,
            ProceduralSkyShaders.SkyToCubemapFragment);
    }

    private (uint vao, uint vbo) CreateCubeMesh()
    {
        float[] vertices =
        [
            -1.0f, -1.0f, -1.0f,  1.0f,  1.0f, -1.0f,  1.0f, -1.0f, -1.0f,
             1.0f,  1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f,  1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,  1.0f, -1.0f,  1.0f,  1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f, -1.0f,  1.0f,  1.0f, -1.0f, -1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f, -1.0f,  1.0f, -1.0f, -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f, -1.0f, -1.0f,  1.0f, -1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,  1.0f, -1.0f, -1.0f,  1.0f,  1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,  1.0f,  1.0f,  1.0f,  1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f, -1.0f,  1.0f, -1.0f, -1.0f,  1.0f, -1.0f,  1.0f,
             1.0f, -1.0f,  1.0f, -1.0f, -1.0f,  1.0f, -1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,  1.0f,  1.0f,  1.0f,  1.0f,  1.0f, -1.0f,
             1.0f,  1.0f,  1.0f, -1.0f,  1.0f, -1.0f, -1.0f,  1.0f,  1.0f
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

    private void RenderCube()
    {
        _gl.BindVertexArray(_cubeVao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
        _gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _skyToCubemapShader?.Dispose();
        _gl.DeleteFramebuffer(_captureFbo);
        _gl.DeleteRenderbuffer(_captureRbo);
        _gl.DeleteVertexArray(_cubeVao);
        _gl.DeleteBuffer(_cubeVbo);

        _disposed = true;
    }
}

/// <summary>
/// Shaders for rendering the procedural atmosphere to a cubemap.
/// </summary>
internal static class ProceduralSkyShaders
{
    public const string SkyToCubemapVertex = """
        #version 450 core
        layout (location = 0) in vec3 aPos;

        out vec3 WorldPos;

        uniform mat4 projection;
        uniform mat4 view;

        void main()
        {
            WorldPos = aPos;
            gl_Position = projection * view * vec4(aPos, 1.0);
        }
        """;

    /// <summary>
    /// Fragment shader that renders the atmosphere model to a cubemap face.
    /// Uses the same atmosphere calculations as the main skybox shader.
    /// </summary>
    public const string SkyToCubemapFragment = """
        #version 450 core
        out vec4 FragColor;
        in vec3 WorldPos;

        uniform vec3 sunDirection;
        uniform float sunIntensity;

        const float PI = 3.14159265359;

        // Earth-like atmosphere constants
        const float PLANET_RADIUS = 6371000.0;
        const float ATMOSPHERE_HEIGHT = 100000.0;
        const float ATMOSPHERE_RADIUS = PLANET_RADIUS + ATMOSPHERE_HEIGHT;

        const float H_RAYLEIGH = 8500.0;
        const float H_MIE = 1200.0;

        const vec3 BETA_RAYLEIGH = vec3(5.802e-6, 13.558e-6, 33.1e-6);
        const vec3 BETA_MIE = vec3(3.996e-6);
        const vec3 BETA_OZONE = vec3(0.650e-6, 1.881e-6, 0.085e-6);

        const float MIE_G = 0.8;

        float getDensityRayleigh(float altitude) {
            return exp(-altitude / H_RAYLEIGH);
        }

        float getDensityMie(float altitude) {
            return exp(-altitude / H_MIE);
        }

        float getDensityOzone(float altitude) {
            float ozonePeak = 25000.0;
            float ozoneWidth = 15000.0;
            return max(0.0, 1.0 - abs(altitude - ozonePeak) / ozoneWidth);
        }

        float phaseRayleigh(float cosTheta) {
            return (3.0 / (16.0 * PI)) * (1.0 + cosTheta * cosTheta);
        }

        float phaseMie(float cosTheta, float g) {
            float g2 = g * g;
            float denom = 1.0 + g2 - 2.0 * g * cosTheta;
            return (1.0 / (4.0 * PI)) * ((1.0 - g2) / (denom * sqrt(max(denom, 1e-6))));
        }

        vec2 raySphereIntersect(vec3 origin, vec3 dir, float radius) {
            float a = dot(dir, dir);
            float b = 2.0 * dot(dir, origin);
            float c = dot(origin, origin) - radius * radius;
            float d = b * b - 4.0 * a * c;
            if (d < 0.0) return vec2(-1.0);
            d = sqrt(d);
            return vec2(-b - d, -b + d) / (2.0 * a);
        }

        vec3 computeTransmittance(vec3 opticalDepth) {
            return exp(-(
                BETA_RAYLEIGH * opticalDepth.x +
                BETA_MIE * 1.11 * opticalDepth.y +
                BETA_OZONE * opticalDepth.z
            ));
        }

        vec3 getTransmittanceToSun(float altitude, float cosZenith) {
            altitude = max(altitude, 0.0);
            float ch;
            if (cosZenith >= 0.0) {
                ch = 1.0 / (cosZenith + 0.15 * pow(93.885 - degrees(acos(cosZenith)), -1.253));
            } else {
                return vec3(0.0);
            }
            
            float odRayleigh = H_RAYLEIGH * getDensityRayleigh(altitude) * ch;
            float odMie = H_MIE * getDensityMie(altitude) * ch;
            float odOzone = 25000.0 * getDensityOzone(altitude) * ch * 0.3;
            
            return exp(-(
                BETA_RAYLEIGH * odRayleigh +
                BETA_MIE * 1.11 * odMie +
                BETA_OZONE * odOzone
            ));
        }

        vec3 computeSkyColor(vec3 viewDir, vec3 sunDir) {
            vec3 rayOrigin = vec3(0.0, PLANET_RADIUS + 1.0, 0.0);
            
            vec2 atmosphereHit = raySphereIntersect(rayOrigin, viewDir, ATMOSPHERE_RADIUS);
            if (atmosphereHit.y < 0.0) return vec3(0.0);
            
            vec2 planetHit = raySphereIntersect(rayOrigin, viewDir, PLANET_RADIUS);
            float rayLength = (planetHit.x > 0.0) ? planetHit.x : atmosphereHit.y;
            
            const int NUM_SAMPLES = 32;
            float stepSize = rayLength / float(NUM_SAMPLES);
            
            vec3 scatteringR = vec3(0.0);
            vec3 scatteringM = vec3(0.0);
            vec3 opticalDepth = vec3(0.0);
            
            float cosTheta = dot(viewDir, sunDir);
            float phaseR = phaseRayleigh(cosTheta);
            float phaseM = phaseMie(cosTheta, MIE_G);
            
            for (int i = 0; i < NUM_SAMPLES; i++) {
                vec3 samplePos = rayOrigin + viewDir * (float(i) + 0.5) * stepSize;
                float altitude = length(samplePos) - PLANET_RADIUS;
                
                if (altitude < 0.0) break;
                
                float densityR = getDensityRayleigh(altitude);
                float densityM = getDensityMie(altitude);
                float densityO = getDensityOzone(altitude);
                
                vec3 localOD = vec3(densityR, densityM, densityO) * stepSize;
                opticalDepth += localOD;
                
                vec3 viewTransmittance = computeTransmittance(opticalDepth);
                
                float sunCosZenith = dot(normalize(samplePos), sunDir);
                vec3 sunTransmittance = getTransmittanceToSun(altitude, sunCosZenith);
                
                vec3 totalTransmittance = viewTransmittance * sunTransmittance;
                
                scatteringR += totalTransmittance * densityR * stepSize;
                scatteringM += totalTransmittance * densityM * stepSize;
            }
            
            return scatteringR * BETA_RAYLEIGH * phaseR + scatteringM * BETA_MIE * phaseM;
        }

        void main()
        {
            vec3 viewDir = normalize(WorldPos);
            vec3 skyColor = computeSkyColor(viewDir, sunDirection) * sunIntensity;
            
            // Add sun disk
            float sunAngle = acos(dot(viewDir, sunDirection));
            float sunRadius = 0.00935; // Angular radius of sun
            if (sunAngle < sunRadius) {
                float sunFade = 1.0 - smoothstep(sunRadius * 0.9, sunRadius, sunAngle);
                vec3 sunColor = getTransmittanceToSun(0.0, sunDirection.y) * sunIntensity * 100.0;
                skyColor += sunColor * sunFade;
            }
            
            FragColor = vec4(skyColor, 1.0);
        }
        """;
}
