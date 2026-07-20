using Microsoft.Extensions.Logging;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Lifecycle;
using System.Reflection;
using System.Text.Json;

namespace SharpCraft.Engine.Lifecycle;

/// <summary>
/// Loads and manages mods.
/// </summary>
public partial class ModLoader(ILogger<ModLoader> logger, ISharpCraftSdk sdk)
{
    private readonly List<IMod> _mods = [];

    /// <summary>
    /// Gets the list of loaded mods.
    /// </summary>
    public IEnumerable<IMod> LoadedMods => _mods;

    /// <summary>
    /// Loads mods from the specified directory.
    /// </summary>
    /// <param name="modsDirectory">The directory to search for mods.</param>
    public void LoadMods(string modsDirectory)
    {
        var discoveredMods = DiscoverMods(modsDirectory);

        try
        {
            var sortedMods = SortByDependencies(discoveredMods);
            _mods.AddRange(sortedMods);
        }
        catch (Exception ex)
        {
            LogDependencySortFailed(ex);
        }
    }

    /// <summary>
    /// Discovers and instantiates every mod under the specified directory, without sorting or
    /// registering them. Malformed manifests and unloadable assemblies are logged and skipped.
    /// </summary>
    /// <param name="modsDirectory">The directory to search for mods.</param>
    /// <returns>The instantiated mods in discovery order.</returns>
    public IReadOnlyList<IMod> DiscoverMods(string modsDirectory)
    {
        var discoveredMods = new List<IMod>();

        if (!Directory.Exists(modsDirectory))
        {
            LogModsDirectoryMissing(modsDirectory);
            return discoveredMods;
        }

        foreach (var dir in Directory.GetDirectories(modsDirectory))
        {
            var manifestPath = Path.Combine(dir, "mod.json");
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            var manifest = TryReadManifest(manifestPath);
            if (manifest == null)
            {
                continue;
            }

            LogModFound(manifest.Name, manifest.Id, manifest.Version);
            discoveredMods.AddRange(InstantiateMods(dir, manifest));
        }

        return discoveredMods;
    }

    private ModManifest? TryReadManifest(string manifestPath)
    {
        try
        {
            return JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            LogManifestLoadFailed(ex, manifestPath);
            return null;
        }
    }

    private IEnumerable<IMod> InstantiateMods(string dir, ModManifest manifest)
    {
        var mods = new List<IMod>();

        foreach (var entrypoint in manifest.Entrypoints)
        {
            // TODO: Implement Tier 2 (scripts)
            if (!entrypoint.EndsWith(".dll"))
            {
                continue;
            }

            var assemblyPath = Path.Combine(dir, entrypoint);
            if (!File.Exists(assemblyPath))
            {
                LogAssemblyNotFound(assemblyPath, manifest.Id);
                continue;
            }

            try
            {
                var assembly = Assembly.LoadFrom(assemblyPath);
                var modTypes = assembly.GetTypes().Where(t => typeof(IMod).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

                foreach (var type in modTypes)
                {
                    if (Activator.CreateInstance(type, sdk) is not IMod mod)
                    {
                        continue;
                    }

                    mod.BaseDirectory = dir;
                    mods.Add(mod);
                }
            }
            catch (Exception ex)
            {
                LogAssemblyLoadFailed(ex, assemblyPath, manifest.Id);
            }
        }

        return mods;
    }

    /// <summary>
    /// Enables all loaded mods.
    /// </summary>
    public void EnableMods()
    {
        foreach (var mod in _mods)
        {
            try
            {
                mod.OnEnable();
                LogModEnabled(mod.Manifest.Id);
            }
            catch (Exception ex)
            {
                LogModEnableFailed(ex, mod.Manifest.Id);
            }
        }
    }

    /// <summary>
    /// Sorts the specified mods by their dependencies.
    /// </summary>
    /// <param name="mods">The mods to sort.</param>
    /// <returns>A list of mods in the correct loading order.</returns>
    /// <exception cref="CircularReferenceException">Thrown when a circular dependency is detected.</exception>
    /// <exception cref="MissingModException">Thrown when a mod dependency is missing.</exception>
    public static IEnumerable<IMod> SortByDependencies(IEnumerable<IMod> mods)
    {
        var modsList = mods.ToList();
        var modDict = modsList.ToDictionary(m => m.Manifest.Id);
        var sorted = new List<IMod>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        void Visit(IMod mod)
        {
            if (visited.Contains(mod.Manifest.Id))
            {
                return;
            }

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