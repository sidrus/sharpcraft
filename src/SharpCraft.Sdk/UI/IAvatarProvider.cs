namespace SharpCraft.Sdk.UI;

/// <summary>
/// Provides avatar information for the current user.
/// </summary>
public interface IAvatarProvider
{
    /// <summary>
    /// Gets the name of the user.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the texture ID of the user's avatar.
    /// </summary>
    IntPtr? AvatarTextureId { get; }
    
    /// <summary>
    /// Whether the avatar is valid and loaded.
    /// </summary>
    bool IsValid { get; }
}
