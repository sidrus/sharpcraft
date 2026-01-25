using System.Numerics;

namespace SharpCraft.Engine.Rendering;

public class BloomFramebuffer : IDisposable
{
    private readonly GL _gl;
    private readonly uint _fbo;
    private readonly List<BloomMip> _mips = new();

    public struct BloomMip
    {
        public Vector2 Size;
        public uint Texture;
    }

    public IReadOnlyList<BloomMip> Mips => _mips;

    public BloomFramebuffer(GL gl, int width, int height, int mipCount = 6)
    {
        _gl = gl;
        _fbo = _gl.GenFramebuffer();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

        var nextWidth = width / 2;
        var nextHeight = height / 2;

        for (int i = 0; i < mipCount; i++)
        {
            var mip = new BloomMip
            {
                Size = new Vector2(nextWidth, nextHeight),
                Texture = _gl.GenTexture()
            };

            _gl.BindTexture(TextureTarget.Texture2D, mip.Texture);
            unsafe
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba16f, (uint)nextWidth, (uint)nextHeight, 0, PixelFormat.Rgba, PixelType.Float, null);
            }
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            _mips.Add(mip);

            nextWidth /= 2;
            nextHeight /= 2;
            if (nextWidth <= 0 || nextHeight <= 0) break;
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Bind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
    }

    public void Unbind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose()
    {
        _gl.DeleteFramebuffer(_fbo);
        foreach (var mip in _mips)
        {
            _gl.DeleteTexture(mip.Texture);
        }
    }
}
