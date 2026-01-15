using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Steamworks;

namespace SharpCraft.Client.Integrations.Steam;

public class AvatarLoader(IWindow window, GL gl) : IDisposable
{
    public uint? AvatarTexture { get; private set; }

    public async Task LoadSteamAvatar()
    {
        if (!SteamClient.IsValid) return;

        // Fetch the Medium avatar (64x64). Large (124x124) is also available.
        var image = await SteamFriends.GetMediumAvatarAsync(SteamClient.SteamId);

        if (image.HasValue)
        {
            if (AvatarTexture.HasValue)
            {
                gl.DeleteTexture(AvatarTexture.Value);
            }
            AvatarTexture = CreateTextureFromRgba(image.Value.Data, image.Value.Width, image.Value.Height);
        }
    }

    private uint CreateTextureFromRgba(byte[] data, uint width, uint height)
    {
        var handle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, handle);

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

        unsafe
        {
            fixed (byte* ptr = data)
            {
                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
        }

        return handle;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool _)
    {
        if (_disposed)
        {
            return;
        }

        if (AvatarTexture.HasValue)
        {
            gl.DeleteTexture(AvatarTexture.Value);
            AvatarTexture = null;
        }

        _disposed = true;
    }

    ~AvatarLoader()
    {
        Dispose(false);
    }

    private bool _disposed;
}