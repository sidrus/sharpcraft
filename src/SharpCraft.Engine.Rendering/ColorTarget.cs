namespace SharpCraft.Engine.Rendering;

/// <summary>
/// A resize-aware, single-color-attachment framebuffer (no depth), used by post-process passes that
/// render to one texture. Centralizes the FBO + texture allocation and the linear/clamp sampler
/// setup those passes previously each hand-rolled.
/// </summary>
public sealed class ColorTarget(GL gl, SizedInternalFormat format) : IDisposable
{
    private uint _fbo;
    private int _width;
    private int _height;
    private bool _disposed;

    /// <summary>Gets the color texture handle (valid after <see cref="EnsureSize"/>).</summary>
    public uint Texture
    {
        get;
        private set;
    }

    /// <summary>Ensures the target exists at the given size, reallocating only when the size changes.</summary>
    public void EnsureSize(int width, int height)
    {
        if (_fbo != 0 && _width == width && _height == height)
        {
            return;
        }

        DeleteResources();

        Texture = gl.CreateTexture(TextureTarget.Texture2D);
        gl.TextureStorage2D(Texture, 1, format, (uint)width, (uint)height);
        gl.TextureParameter(Texture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TextureParameter(Texture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.TextureParameter(Texture, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TextureParameter(Texture, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        _fbo = gl.CreateFramebuffer();
        gl.NamedFramebufferTexture(_fbo, FramebufferAttachment.ColorAttachment0, Texture, 0);
        gl.NamedFramebufferDrawBuffer(_fbo, ColorBuffer.ColorAttachment0);

        _width = width;
        _height = height;
    }

    /// <summary>Binds this target's framebuffer for rendering.</summary>
    public void Bind()
    {
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        DeleteResources();
        _disposed = true;
    }

    private void DeleteResources()
    {
        if (_fbo != 0)
        {
            gl.DeleteFramebuffer(_fbo);
            gl.DeleteTexture(Texture);
            _fbo = 0;
        }
    }
}