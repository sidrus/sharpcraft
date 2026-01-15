namespace SharpCraft.Sdk.Rendering;

public readonly record struct Material
{
    public string AlbedoPath { get; }
    public string? NormalPath { get; init; }
    public string? AmbientOcclusionPath { get; init; }
    public string? MetallicPath { get; init; }
    public string? RoughnessPath { get; init; }

    public Material(string albedoPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(albedoPath);
        AlbedoPath = albedoPath;
    }

    public void Deconstruct(
        out string albedoPath,
        out string? normalPath,
        out string? ambientOcclusionPath,
        out string? metallicPath,
        out string? roughnessPath)
    {
        albedoPath = AlbedoPath;
        normalPath = NormalPath;
        ambientOcclusionPath = AmbientOcclusionPath;
        metallicPath = MetallicPath;
        roughnessPath = RoughnessPath;
    }
}