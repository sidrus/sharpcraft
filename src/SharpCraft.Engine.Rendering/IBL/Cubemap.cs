namespace SharpCraft.Engine.Rendering.IBL;

/// <summary>
/// Represents an OpenGL cubemap texture with support for mipmaps.
/// Used for environment maps, irradiance maps, and prefiltered specular maps.
/// </summary>
public sealed class Cubemap : IDisposable
{
    private readonly GL _gl;
    private bool _disposed;

    public uint Handle { get; }
    public int Size { get; }
    public int MipLevels { get; }

    /// <summary>
    /// Creates a new cubemap texture.
    /// </summary>
    /// <param name="gl">OpenGL context</param>
    /// <param name="size">Size of each face in pixels (must be power of 2)</param>
    /// <param name="internalFormat">Internal texture format</param>
    /// <param name="generateMipmaps">Whether to allocate mipmap levels</param>
    public Cubemap(GL gl, int size, InternalFormat internalFormat = InternalFormat.Rgba16f, bool generateMipmaps = false)
    {
        _gl = gl;
        Size = size;
        MipLevels = generateMipmaps ? (int)Math.Floor(Math.Log2(size)) + 1 : 1;

        Handle = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.TextureCubeMap, Handle);

        // Allocate storage for all faces and mip levels
        for (var i = 0; i < 6; i++)
        {
            var target = TextureTarget.TextureCubeMapPositiveX + i;
            for (var mip = 0; mip < MipLevels; mip++)
            {
                var mipSize = (uint)(size >> mip);
                _gl.TexImage2D(target, mip, internalFormat, mipSize, mipSize, 0,
                    PixelFormat.Rgba, PixelType.Float, ReadOnlySpan<byte>.Empty);
            }
        }

        // Set texture parameters
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
            generateMipmaps ? (int)TextureMinFilter.LinearMipmapLinear : (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

        if (generateMipmaps)
        {
            // Enable seamless cubemap filtering for better quality at edges
            _gl.Enable(EnableCap.TextureCubeMapSeamless);
        }

        _gl.BindTexture(TextureTarget.TextureCubeMap, 0);
    }

    /// <summary>
    /// Binds the cubemap to the specified texture unit.
    /// </summary>
    public void Bind(uint unit = 0)
    {
        _gl.ActiveTexture(TextureUnit.Texture0 + (int)unit);
        _gl.BindTexture(TextureTarget.TextureCubeMap, Handle);
    }

    /// <summary>
    /// Generates mipmaps for the cubemap.
    /// </summary>
    public void GenerateMipmaps()
    {
        _gl.BindTexture(TextureTarget.TextureCubeMap, Handle);
        _gl.GenerateMipmap(TextureTarget.TextureCubeMap);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _gl.DeleteTexture(Handle);
        _disposed = true;
    }

    ~Cubemap()
    {
        // Note: OpenGL resources should be deleted on the GL thread
        // This finalizer is a safety net but may not work correctly
    }
}
