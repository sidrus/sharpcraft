namespace SharpCraft.Engine.Rendering;

/// <summary>
/// A resize-aware, single-color-attachment framebuffer (no depth), used by post-process passes that
/// render to one texture. Centralizes the FBO + texture allocation and the linear/clamp sampler
/// setup those passes previously each hand-rolled.
/// </summary>
public sealed class ColorTarget(GL gl, SizedInternalFormat format) : IDisposable
{
    private uint _fbo;
    private uint _texture;
    private int _width;
    private int _height;
    private bool _disposed;

    /// <summary>Gets the color texture handle (valid after <see cref="EnsureSize"/>).</summary>
    public uint Texture => _texture;

    /// <summary>Ensures the target exists at the given size, reallocating only when the size changes.</summary>
    public void EnsureSize(int width, int height)
    {
        if (_fbo != 0 && _width == width && _height == height)
        {
            return;
        }

        DeleteResources();

        _texture = gl.CreateTexture(TextureTarget.Texture2D);
        gl.TextureStorage2D(_texture, 1, format, (uint)width, (uint)height);
        gl.TextureParameter(_texture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TextureParameter(_texture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.TextureParameter(_texture, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TextureParameter(_texture, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        _fbo = gl.CreateFramebuffer();
        gl.NamedFramebufferTexture(_fbo, FramebufferAttachment.ColorAttachment0, _texture, 0);
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
            gl.DeleteTexture(_texture);
            _fbo = 0;
        }
    }
}
