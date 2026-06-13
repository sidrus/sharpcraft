namespace SharpCraft.Engine.Rendering;

public class Framebuffer : IDisposable
{
    private readonly GL _gl;
    private readonly uint _handle;
    private readonly uint _textureHandle;
    private readonly uint _depthTextureHandle;

    public uint Handle => _handle;
    public uint TextureHandle => _textureHandle;
    public uint DepthTextureHandle => _depthTextureHandle;
    public int Width { get; }
    public int Height { get; }

    public Framebuffer(GL gl, int width, int height, bool hdr = false)
    {
        _gl = gl;
        Width = width;
        Height = height;

        _handle = _gl.CreateFramebuffer();

        // Color attachment. HDR keeps radiance in fp16 linear (research §5.1); SDR is 8-bit.
        var colorFormat = hdr ? SizedInternalFormat.Rgba16f : SizedInternalFormat.Rgba8;
        _textureHandle = _gl.CreateTexture(TextureTarget.Texture2D);
        _gl.TextureStorage2D(_textureHandle, 1, colorFormat, (uint)width, (uint)height);
        _gl.TextureParameter(_textureHandle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TextureParameter(_textureHandle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TextureParameter(_textureHandle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TextureParameter(_textureHandle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.NamedFramebufferTexture(_handle, FramebufferAttachment.ColorAttachment0, _textureHandle, 0);

        // Depth attachment. Float depth is required for reversed-Z precision (research §12.2).
        _depthTextureHandle = _gl.CreateTexture(TextureTarget.Texture2D);
        _gl.TextureStorage2D(_depthTextureHandle, 1, SizedInternalFormat.DepthComponent32f, (uint)width, (uint)height);
        _gl.TextureParameter(_depthTextureHandle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        _gl.TextureParameter(_depthTextureHandle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        _gl.TextureParameter(_depthTextureHandle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TextureParameter(_depthTextureHandle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.NamedFramebufferTexture(_handle, FramebufferAttachment.DepthAttachment, _depthTextureHandle, 0);

        var status = (FramebufferStatus)_gl.CheckNamedFramebufferStatus(_handle, FramebufferTarget.Framebuffer);
        if (status != FramebufferStatus.Complete)
        {
            throw new Exception($"Framebuffer is not complete! Status: {status}");
        }
    }

    public void Bind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _handle);
    }

    public void Unbind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose()
    {
        _gl.DeleteFramebuffer(_handle);
        _gl.DeleteTexture(_textureHandle);
        _gl.DeleteTexture(_depthTextureHandle);
    }
}
