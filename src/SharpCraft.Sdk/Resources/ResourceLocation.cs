namespace SharpCraft.Sdk.Resources;

/// <summary>
/// Represents a namespaced resource location (e.g., "minecraft:dirt").
/// </summary>
/// <param name="Namespace">The namespace of the resource (e.g., "minecraft").</param>
/// <param name="Path">The path of the resource within the namespace (e.g., "dirt").</param>
public readonly record struct ResourceLocation(string Namespace, string Path)
{
    public override string ToString() => $"{Namespace}:{Path}";

    public static implicit operator ResourceLocation(string location) => Parse(location);

    public static explicit operator string(ResourceLocation location) => location.ToString();

    public static ResourceLocation Parse(string location) =>
        TryParse(location, out var result) && result is { } value
            ? value
            : throw new ArgumentException($"Invalid resource location format: {location}. Expected 'namespace:path'.");

    public static bool TryParse(string location, out ResourceLocation? result)
    {
        var parts = location.Split(':');
        if (parts.Length != 2)
        {
            result = null;
            return false;
        }

        result = new ResourceLocation(parts[0], parts[1]);
        return true;
    }
}
