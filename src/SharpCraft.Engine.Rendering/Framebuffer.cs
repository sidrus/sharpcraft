namespace SharpCraft.Engine.Rendering;

public class Framebuffer : IDisposable
{
    private readonly GL _gl;
    private readonly uint _handle;
    private readonly uint _textureHandle;
    private readonly uint _renderbufferHandle;

    private readonly uint _depthTextureHandle;

    public uint Handle => _handle;
    public uint TextureHandle => _textureHandle;
    public uint DepthTextureHandle => _depthTextureHandle;

    public Framebuffer(GL gl, int width, int height, bool hdr = false)
    {
        _gl = gl;

        // Create Framebuffer
        _handle = _gl.GenFramebuffer();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _handle);

        // Create Color Attachment Texture
        _textureHandle = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _textureHandle);
        unsafe
        {
            var internalFormat = hdr ? InternalFormat.Rgba16f : InternalFormat.Rgba;
            var type = hdr ? PixelType.Float : PixelType.UnsignedByte;
            _gl.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, (uint)width, (uint)height, 0, PixelFormat.Rgba, type, null);
        }
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _textureHandle, 0);

        // Create Depth Texture instead of Renderbuffer
        _depthTextureHandle = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _depthTextureHandle);
        unsafe
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent24, (uint)width, (uint)height, 0, PixelFormat.DepthComponent, PixelType.Float, null);
        }
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _depthTextureHandle, 0);

        if ((FramebufferStatus)_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.FramebufferComplete)
        {
            throw new Exception("Framebuffer is not complete!");
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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
        _gl.DeleteRenderbuffer(_renderbufferHandle);
    }
}
