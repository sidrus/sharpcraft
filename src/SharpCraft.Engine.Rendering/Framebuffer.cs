namespace SharpCraft.Engine.Rendering;

public class Framebuffer : IDisposable
{
    private readonly GL _gl;

    public uint Handle
    {
        get;
    }

    public uint TextureHandle
    {
        get;
    }

    public uint DepthTextureHandle
    {
        get;
    }

    public int Width
    {
        get;
    }
    public int Height
    {
        get;
    }

    public Framebuffer(GL gl, int width, int height, bool hdr = false)
    {
        _gl = gl;
        Width = width;
        Height = height;

        Handle = _gl.CreateFramebuffer();

        // Color attachment. HDR keeps radiance in fp16 linear (research §5.1); SDR is 8-bit.
        var colorFormat = hdr ? SizedInternalFormat.Rgba16f : SizedInternalFormat.Rgba8;
        TextureHandle = _gl.CreateTexture(TextureTarget.Texture2D);
        _gl.TextureStorage2D(TextureHandle, 1, colorFormat, (uint)width, (uint)height);
        _gl.TextureParameter(TextureHandle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TextureParameter(TextureHandle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TextureParameter(TextureHandle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TextureParameter(TextureHandle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.NamedFramebufferTexture(Handle, FramebufferAttachment.ColorAttachment0, TextureHandle, 0);

        // Depth attachment. Float depth is required for reversed-Z precision (research §12.2).
        DepthTextureHandle = _gl.CreateTexture(TextureTarget.Texture2D);
        _gl.TextureStorage2D(DepthTextureHandle, 1, SizedInternalFormat.DepthComponent32f, (uint)width, (uint)height);
        _gl.TextureParameter(DepthTextureHandle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        _gl.TextureParameter(DepthTextureHandle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        _gl.TextureParameter(DepthTextureHandle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TextureParameter(DepthTextureHandle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.NamedFramebufferTexture(Handle, FramebufferAttachment.DepthAttachment, DepthTextureHandle, 0);

        var status = (FramebufferStatus)_gl.CheckNamedFramebufferStatus(Handle, FramebufferTarget.Framebuffer);
        if (status != FramebufferStatus.Complete)
        {
            throw new Exception($"Framebuffer is not complete! Status: {status}");
        }
    }

    public void Bind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
    }

    public void Unbind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose()
    {
        _gl.DeleteFramebuffer(Handle);
        _gl.DeleteTexture(TextureHandle);
        _gl.DeleteTexture(DepthTextureHandle);
    }
}