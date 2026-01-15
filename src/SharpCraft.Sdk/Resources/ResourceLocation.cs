namespace SharpCraft.Sdk.Resources;

/// <summary>
/// Represents a namespaced resource location (e.g., "minecraft:dirt").
/// </summary>
/// <param name="Namespace">The namespace of the resource (e.g., "minecraft").</param>
/// <param name="Path">The path of the resource within the namespace (e.g., "dirt").</param>
public record ResourceLocation(string Namespace, string Path)
{
    public override string ToString() => $"{Namespace}:{Path}";

    public static ResourceLocation Parse(string location)
    {
        var parts = location.Split(':');
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid resource location format: {location}. Expected 'namespace:path'.");

        return new ResourceLocation(parts[0], parts[1]);
    }

    public static bool TryParse(string location, out ResourceLocation? result)
    {
        try
        {
            result = Parse(location);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
}
