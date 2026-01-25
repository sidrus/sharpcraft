namespace SharpCraft.Engine.Rendering;

/// <summary>
/// G-Buffer for deferred rendering with multiple render targets.
/// Contains: AlbedoAO, Normal, Material (Metallic/Roughness), Position
/// </summary>
public class GBuffer : IDisposable
{
    private readonly GL _gl;
    private readonly uint _handle;
    private readonly uint _albedoAoTexture;
    private readonly uint _normalTexture;
    private readonly uint _materialTexture;
    private readonly uint _positionTexture;
    private readonly uint _depthTexture;
    private bool _disposed;

    public uint Handle => _handle;
    public uint AlbedoAoTexture => _albedoAoTexture;
    public uint NormalTexture => _normalTexture;
    public uint MaterialTexture => _materialTexture;
    public uint PositionTexture => _positionTexture;
    public uint DepthTexture => _depthTexture;
    public int Width { get; }
    public int Height { get; }

    public GBuffer(GL gl, int width, int height)
    {
        _gl = gl;
        Width = width;
        Height = height;

        _handle = _gl.GenFramebuffer();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _handle);

        // Albedo + AO (RGBA16F for HDR albedo, A channel for AO)
        _albedoAoTexture = CreateColorTexture(width, height, InternalFormat.Rgba16f, FramebufferAttachment.ColorAttachment0);

        // Normal (RGB16F for world-space normals)
        _normalTexture = CreateColorTexture(width, height, InternalFormat.Rgba16f, FramebufferAttachment.ColorAttachment1);

        // Material (R: Metallic, G: Roughness)
        _materialTexture = CreateColorTexture(width, height, InternalFormat.Rgba16f, FramebufferAttachment.ColorAttachment2);

        // Position + Fragment Distance (RGB: Position, A: Distance)
        _positionTexture = CreateColorTexture(width, height, InternalFormat.Rgba32f, FramebufferAttachment.ColorAttachment3);

        // Depth texture
        _depthTexture = CreateDepthTexture(width, height);

        // Specify which color attachments to draw to
        var attachments = new[]
        {
            DrawBufferMode.ColorAttachment0,
            DrawBufferMode.ColorAttachment1,
            DrawBufferMode.ColorAttachment2,
            DrawBufferMode.ColorAttachment3
        };
        _gl.DrawBuffers(attachments);

        var status = (FramebufferStatus)_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferStatus.FramebufferComplete)
        {
            throw new InvalidOperationException($"G-Buffer framebuffer is not complete! Status: {status}");
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private uint CreateColorTexture(int width, int height, InternalFormat format, FramebufferAttachment attachment)
    {
        var texture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, texture);

        unsafe
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, format, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.Float, null);
        }

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment, TextureTarget.Texture2D, texture, 0);

        return texture;
    }

    private uint CreateDepthTexture(int width, int height)
    {
        var texture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, texture);

        unsafe
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent24, (uint)width, (uint)height, 0, PixelFormat.DepthComponent, PixelType.Float, null);
        }

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, texture, 0);

        return texture;
    }

    public void Bind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _handle);
        _gl.Viewport(0, 0, (uint)Width, (uint)Height);
    }

    public void Unbind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    /// <summary>
    /// Binds all G-Buffer textures to sequential texture units starting from the specified unit.
    /// </summary>
    public void BindTextures(uint startUnit)
    {
        _gl.ActiveTexture(TextureUnit.Texture0 + (int)startUnit);
        _gl.BindTexture(TextureTarget.Texture2D, _albedoAoTexture);

        _gl.ActiveTexture(TextureUnit.Texture0 + (int)startUnit + 1);
        _gl.BindTexture(TextureTarget.Texture2D, _normalTexture);

        _gl.ActiveTexture(TextureUnit.Texture0 + (int)startUnit + 2);
        _gl.BindTexture(TextureTarget.Texture2D, _materialTexture);

        _gl.ActiveTexture(TextureUnit.Texture0 + (int)startUnit + 3);
        _gl.BindTexture(TextureTarget.Texture2D, _positionTexture);

        _gl.ActiveTexture(TextureUnit.Texture0 + (int)startUnit + 4);
        _gl.BindTexture(TextureTarget.Texture2D, _depthTexture);
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
            _gl.DeleteFramebuffer(_handle);
            _gl.DeleteTexture(_albedoAoTexture);
            _gl.DeleteTexture(_normalTexture);
            _gl.DeleteTexture(_materialTexture);
            _gl.DeleteTexture(_positionTexture);
            _gl.DeleteTexture(_depthTexture);
        }

        _disposed = true;
    }

    ~GBuffer()
    {
        Dispose(false);
    }
}
