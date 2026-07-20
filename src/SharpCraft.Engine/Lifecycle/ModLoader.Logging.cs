using Microsoft.Extensions.Logging;

namespace SharpCraft.Engine.Lifecycle;

public partial class ModLoader
{
    [LoggerMessage(LogLevel.Warning, "Mods directory {directory} does not exist")]
    partial void LogModsDirectoryMissing(string directory);

    [LoggerMessage(LogLevel.Information, "Found mod: {modName} ({modId}) v{version}")]
    partial void LogModFound(string modName, string modId, string version);

    [LoggerMessage(LogLevel.Error, "Failed to load assembly {path} for mod {modId}")]
    partial void LogAssemblyLoadFailed(Exception ex, string path, string modId);

    [LoggerMessage(LogLevel.Error, "Assembly {path} not found for mod {modId}")]
    partial void LogAssemblyNotFound(string path, string modId);

    [LoggerMessage(LogLevel.Error, "Failed to load mod manifest from {path}")]
    partial void LogManifestLoadFailed(Exception ex, string path);

    [LoggerMessage(LogLevel.Error, "Failed to sort mods by dependencies")]
    partial void LogDependencySortFailed(Exception ex);

    [LoggerMessage(LogLevel.Information, "Enabled mod: {modId}")]
    partial void LogModEnabled(string modId);

    [LoggerMessage(LogLevel.Error, "Failed to enable mod: {modId}")]
    partial void LogModEnableFailed(Exception ex, string modId);
}