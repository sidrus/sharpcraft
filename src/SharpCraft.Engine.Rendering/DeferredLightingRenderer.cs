using SharpCraft.Engine.Rendering.Shaders;

namespace SharpCraft.Engine.Rendering;

/// <summary>
/// Renders the deferred lighting pass using G-Buffer data.
/// </summary>
public class DeferredLightingRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly uint _quadVao;
    private readonly uint _quadVbo;
    private bool _disposed;

    public DeferredLightingRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(gl, Shaders.Shaders.LightingVertex, Shaders.Shaders.LightingFragment);

        // Create fullscreen quad
        float[] quadVertices =
        [
            // positions   // texCoords
            -1.0f,  1.0f,  0.0f, 1.0f,
            -1.0f, -1.0f,  0.0f, 0.0f,
             1.0f, -1.0f,  1.0f, 0.0f,

            -1.0f,  1.0f,  0.0f, 1.0f,
             1.0f, -1.0f,  1.0f, 0.0f,
             1.0f,  1.0f,  1.0f, 1.0f
        ];

        _quadVao = _gl.GenVertexArray();
        _quadVbo = _gl.GenBuffer();

        _gl.BindVertexArray(_quadVao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVbo);

        unsafe
        {
            fixed (float* v = quadVertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(quadVertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }
        }

        // Position attribute
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

        // TexCoords attribute
        _gl.EnableVertexAttribArray(1);
        unsafe
        {
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
        }

        _gl.BindVertexArray(0);
    }

    public void Render(
        GBuffer gBuffer,
        uint shadowMap,
        RenderContext context)
    {
        _shader.Use();

        // Bind G-Buffer textures
        gBuffer.BindTextures(0);
        _shader.SetUniform("gAlbedoAO", 0);
        _shader.SetUniform("gNormal", 1);
        _shader.SetUniform("gMaterial", 2);
        _shader.SetUniform("gPosition", 3);
        _shader.SetUniform("gDepth", 4);

        // Bind shadow map
        _gl.ActiveTexture(TextureUnit.Texture5);
        _gl.BindTexture(TextureTarget.Texture2D, shadowMap);
        _shader.SetUniform("shadowMap", 5);

        // IBL settings
        var useIbl = context.UseIBL && context.IrradianceMap != 0 && context.PrefilterMap != 0 && context.BrdfLut != 0;
        _shader.SetUniform("useIBL", useIbl ? 1 : 0);

        if (useIbl)
        {
            // Bind IBL textures
            _gl.ActiveTexture(TextureUnit.Texture6);
            _gl.BindTexture(TextureTarget.TextureCubeMap, context.IrradianceMap);
            _shader.SetUniform("irradianceMap", 6);

            _gl.ActiveTexture(TextureUnit.Texture7);
            _gl.BindTexture(TextureTarget.TextureCubeMap, context.PrefilterMap);
            _shader.SetUniform("prefilterMap", 7);

            _gl.ActiveTexture(TextureUnit.Texture8);
            _gl.BindTexture(TextureTarget.Texture2D, context.BrdfLut);
            _shader.SetUniform("brdfLUT", 8);
        }

        // Draw fullscreen quad
        _gl.BindVertexArray(_quadVao);
        _gl.Disable(EnableCap.DepthTest);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        _gl.Enable(EnableCap.DepthTest);
        _gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _shader.Dispose();
            _gl.DeleteVertexArray(_quadVao);
            _gl.DeleteBuffer(_quadVbo);
        }

        _disposed = true;
    }

    ~DeferredLightingRenderer()
    {
        Dispose(false);
    }
}
