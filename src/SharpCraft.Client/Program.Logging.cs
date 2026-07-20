using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace SharpCraft.Client;

internal static partial class ProgramLog
{
    [LoggerMessage(LogLevel.Information, "SharpCraft starting...")]
    public static partial void Starting(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Process Architecture: {arch}")]
    public static partial void ProcessArchitecture(ILogger logger, Architecture arch);

    [LoggerMessage(LogLevel.Information, "Runtime Directory: {dir}")]
    public static partial void RuntimeDirectory(ILogger logger, string dir);

    [LoggerMessage(LogLevel.Information, "Loading mods...")]
    public static partial void LoadingMods(ILogger logger);

    [LoggerMessage(LogLevel.Critical, "No mods found. Ensure SharpCraft is installed correctly")]
    public static partial void NoModsFound(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Initializing Steam...")]
    public static partial void InitializingSteam(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Steam initialized: {name}")]
    public static partial void SteamInitialized(ILogger logger, string name);

    [LoggerMessage(LogLevel.Error, "Could not initialize Steam. Ensure Steam is running")]
    public static partial void SteamInitFailed(ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "Generating world...")]
    public static partial void GeneratingWorld(ILogger logger);

    [LoggerMessage(LogLevel.Information, "World generation complete")]
    public static partial void WorldGenerationComplete(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Creating game window...")]
    public static partial void CreatingWindow(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Entering game loop...")]
    public static partial void EnteringGameLoop(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Shutting down...")]
    public static partial void ShuttingDown(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Shutdown complete")]
    public static partial void ShutdownComplete(ILogger logger);
}