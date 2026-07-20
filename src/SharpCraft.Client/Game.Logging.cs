using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;

namespace SharpCraft.Client;

public partial class Game
{
    [LoggerMessage(LogLevel.Information, "Creating OpenGL context...")]
    partial void LogCreatingOpenglContext();

    [LoggerMessage(LogLevel.Information, "OpenGL context created.")]
    partial void LogOpenglContextCreated();

    [LoggerMessage(LogLevel.Error, "Failed to load game")]
    partial void LogGameLoadFailed(Exception ex);

    [LoggerMessage(LogLevel.Error, "GL [{source}/{type}] id={id}: {message}")]
    partial void LogGlError(GLEnum source, GLEnum type, int id, string? message);

    [LoggerMessage(LogLevel.Warning, "GL [{source}/{type}] id={id}: {message}")]
    partial void LogGlWarning(GLEnum source, GLEnum type, int id, string? message);

    [LoggerMessage(LogLevel.Information, "GL [{source}/{type}] id={id}: {message}")]
    partial void LogGlInfo(GLEnum source, GLEnum type, int id, string? message);

    [LoggerMessage(LogLevel.Information, "Normal mapping toggled: {state}")]
    partial void LogNormalMappingToggledState(bool state);

    [LoggerMessage(LogLevel.Information, "Metallic mapping toggled: {state}")]
    partial void LogMetallicMappingToggledState(bool state);

    [LoggerMessage(LogLevel.Information, "Roughness mapping toggled: {state}")]
    partial void LogRoughnessMappingToggledState(bool state);

    [LoggerMessage(LogLevel.Information, "AO mapping toggled: {state}")]
    partial void LogAoMappingToggledState(bool state);

    [LoggerMessage(LogLevel.Error, "World update task failed")]
    partial void LogWorldUpdateTaskFailed(Exception ex);

    [LoggerMessage(LogLevel.Information, "Placed torch at ({x}, {y}, {z}); {count} torch(es) total")]
    partial void LogTorchPlaced(float x, float y, float z, int count);

    [LoggerMessage(LogLevel.Debug, "MakeCurrent failed during disposal")]
    partial void LogMakeCurrentFailed(Exception ex);
}