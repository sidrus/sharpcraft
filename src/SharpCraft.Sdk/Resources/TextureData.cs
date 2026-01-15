namespace SharpCraft.Sdk.Resources;

/// <summary>
/// Represents raw texture data.
/// </summary>
public record TextureData(
    int Width,
    int Height,
    byte[] Data,
    byte[]? NormalData = null,
    byte[]? AoData = null,
    byte[]? SpecularData = null
);
