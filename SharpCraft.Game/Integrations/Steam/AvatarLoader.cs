using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Steamworks;

namespace SharpCraft.Game.Integrations.Steam;

public class AvatarLoader(IWindow window, GL gl) : IDisposable
{
    public uint? AvatarTexture { get; private set; }
    private Steamworks.Data.Image? _pendingAvatar;
    private readonly object _lock = new();

    public async Task LoadSteamAvatar()
    {
        if (!SteamClient.IsValid) return;

        // Fetch the Medium avatar (64x64). Large (124x124) is also available.
        var image = await SteamFriends.GetMediumAvatarAsync(SteamClient.SteamId);

        if (image.HasValue)
        {
            lock (_lock)
            {
                _pendingAvatar = image.Value;
            }
        }
    }

    public void Update()
    {
        Steamworks.Data.Image? toLoad = null;
        lock (_lock)
        {
            if (_pendingAvatar.HasValue)
            {
                toLoad = _pendingAvatar;
                _pendingAvatar = null;
            }
        }

        if (toLoad.HasValue)
        {
            if (AvatarTexture.HasValue)
            {
                gl.DeleteTexture(AvatarTexture.Value);
            }
            AvatarTexture = CreateTextureFromRgba(toLoad.Value.Data, toLoad.Value.Width, toLoad.Value.Height);
        }
    }

    private uint CreateTextureFromRgba(byte[] data, uint width, uint height)
    {
        var handle = gl!.GenTexture();
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
        if (AvatarTexture.HasValue)
        {
            gl.DeleteTexture(AvatarTexture.Value);
            AvatarTexture = null;
        }
    }
}