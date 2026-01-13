using System.Text.Json;
using Microsoft.Extensions.Logging;
using SharpCraft.Sdk.Lifecycle;

namespace SharpCraft.Sdk.Runtime.Lifecycle;

/// <summary>
/// Loads and manages mods.
/// </summary>
public class ModLoader(ILogger<ModLoader> logger)
{
    private readonly List<IMod> _mods = [];

    public IEnumerable<IMod> LoadedMods => _mods;

    public void LoadMods(string modsDirectory)
    {
        if (!Directory.Exists(modsDirectory))
        {
            logger.LogWarning("Mods directory {Directory} does not exist", modsDirectory);
            return;
        }

        foreach (var dir in Directory.GetDirectories(modsDirectory))
        {
            var manifestPath = Path.Combine(dir, "mod.json");
            if (File.Exists(manifestPath))
            {
                try
                {
                    var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (manifest != null)
                    {
                        logger.LogInformation("Found mod: {ModName} ({ModId}) v{Version}", manifest.Name, manifest.Id, manifest.Version);
                        // TODO: Implement actual loading of assemblies (Tier 1) or scripts (Tier 2)
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to load mod manifest from {Path}", manifestPath);
                }
            }
        }
    }

    public void EnableMods()
    {
        foreach (var mod in _mods)
        {
            try
            {
                mod.OnEnable();
                logger.LogInformation("Enabled mod: {ModId}", mod.Manifest.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to enable mod: {ModId}", mod.Manifest.Id);
            }
        }
    }

    public static IEnumerable<IMod> SortByDependencies(IEnumerable<IMod> mods)
    {
        var modsList = mods.ToList();
        var modDict = modsList.ToDictionary(m => m.Manifest.Id);
        var sorted = new List<IMod>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        void Visit(IMod mod)
        {
            if (visited.Contains(mod.Manifest.Id)) return;
            if (visiting.Contains(mod.Manifest.Id))
            {
                throw new CircularReferenceException($"Detected circular dependency involving mod '{mod.Manifest.Id}'.");
            }

            visiting.Add(mod.Manifest.Id);

            foreach (var depId in mod.Manifest.Dependencies)
            {
                if (!modDict.TryGetValue(depId, out var depMod))
                {
                    throw new MissingModException($"Mod '{mod.Manifest.Id}' depends on missing mod '{depId}'.");
                }
                Visit(depMod);
            }

            visiting.Remove(mod.Manifest.Id);
            visited.Add(mod.Manifest.Id);
            sorted.Add(mod);
        }

        foreach (var mod in modsList)
        {
            Visit(mod);
        }

        return sorted;
    }
}

public class MissingModException(string message) : Exception(message)
{
    public void Deconstruct(out string message)
    {
        message = Message;
    }
}

public class CircularReferenceException(string message) : Exception(message)
{
    public void Deconstruct(out string message)
    {
        message = Message;
    }
}
