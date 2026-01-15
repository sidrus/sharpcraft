namespace SharpCraft.Sdk.Diagnostics;

/// <summary>
/// Provides diagnostic metrics for the engine and world.
/// </summary>
public interface IDiagnosticsProvider
{
    Metric Fps { get; }
    Metric CpuUsage { get; }
    Metric RamUsage { get; }
    Metric GcMemory { get; }
    Metric LoadedChunks { get; }
    Metric MeshQueue { get; }
    Metric ActiveLights { get; }
}
