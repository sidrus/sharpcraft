using System.Diagnostics;
using SharpCraft.Client.Rendering;
using SharpCraft.Client.Rendering.Lighting;
using SharpCraft.Engine.Universe;

namespace SharpCraft.Client.UI.Debug.Diagnostics;

public class DiagnosticsManager
{
    private readonly Process _process = Process.GetCurrentProcess();
    private TimeSpan _lastCpuTime = TimeSpan.Zero;
    private readonly Stopwatch _cpuStopwatch = Stopwatch.StartNew();
    
    // History for 5 minutes at 10Hz = 3000 samples
    private const int MaxSamples = 3000;
    private double _sampleTimer;
    private const double SampleInterval = 0.1; // 100ms

    public Metric Fps { get; } = new("FPS", MaxSamples);
    public Metric CpuUsage { get; } = new("CPU %", MaxSamples);
    public Metric RamUsage { get; } = new("RAM (MB)", MaxSamples);
    public Metric GcMemory { get; } = new("GC Mem (MB)", MaxSamples);
    public Metric LoadedChunks { get; } = new("Chunks", MaxSamples);
    public Metric MeshQueue { get; } = new("Mesh Queue", MaxSamples);
    public Metric ActiveLights { get; } = new("Lights", MaxSamples);

    public void Update(double deltaTime, World world, ChunkMeshManager meshManager, LightingSystem lighting)
    {
        _sampleTimer += deltaTime;
        
        if (_sampleTimer >= SampleInterval)
        {
            _sampleTimer -= SampleInterval;
            RecordSamples(deltaTime, world, meshManager, lighting);
        }
    }

    private void RecordSamples(double deltaTime, World world, ChunkMeshManager meshManager, LightingSystem lighting)
    {
        // FPS
        Fps.AddSample((float)(1.0 / deltaTime));

        // CPU
        var currentCpuTime = _process.TotalProcessorTime;
        var elapsed = _cpuStopwatch.Elapsed;
        if (elapsed.TotalMilliseconds > 0)
        {
            var cpuUsage = (float)((currentCpuTime - _lastCpuTime).TotalMilliseconds / (elapsed.TotalMilliseconds * Environment.ProcessorCount)) * 100f;
            CpuUsage.AddSample(cpuUsage);
        }
        _lastCpuTime = currentCpuTime;
        _cpuStopwatch.Restart();

        // RAM
        RamUsage.AddSample(_process.WorkingSet64 / 1024f / 1024f);

        // GC
        GcMemory.AddSample(GC.GetTotalMemory(false) / 1024f / 1024f);

        // World
        LoadedChunks.AddSample(world.GetLoadedChunks().Count());

        // Rendering/Mesh
        MeshQueue.AddSample(meshManager.DirtyChunksCount + meshManager.ProcessingChunksCount);
        
        // Active Lights
        ActiveLights.AddSample(lighting.GetActivePointLights().Count() + lighting.GetActiveSpotLights().Count() + 1); // +1 for Sun
    }
}
