namespace SharpCraft.Sdk.Resources;

/// <summary>
/// Represents raw texture data.
/// </summary>
/// <param name="Width">The width of the texture in pixels.</param>
/// <param name="Height">The height of the texture in pixels.</param>
/// <param name="Data">The raw RGBA pixel data.</param>
/// <param name="NormalData">Optional raw normal map data.</param>
/// <param name="AoData">Optional raw ambient occlusion data.</param>
/// <param name="SpecularData">Optional raw specular map data.</param>
public record TextureData(
    int Width,
    int Height,
    byte[] Data,
    byte[]? NormalData = null,
    byte[]? AoData = null,
    byte[]? SpecularData = null,
    byte[]? MetallicData = null,
    byte[]? RoughnessData = null
);
