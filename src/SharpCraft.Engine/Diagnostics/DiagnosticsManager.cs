using System.Diagnostics;
using SharpCraft.Sdk.Diagnostics;

namespace SharpCraft.Engine.Diagnostics;

public class DiagnosticsManager : IDiagnosticsProvider
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
    public Metric Velocity { get; } = new("Velocity", MaxSamples);
    public string GameTime { get; private set; } = "06:00 AM";

    public void Update(double deltaTime, int loadedChunks, int meshQueue, int activeLights, float velocity, string gameTime = "")
    {
        _sampleTimer += deltaTime;

        if (!string.IsNullOrEmpty(gameTime))
        {
            GameTime = gameTime;
        }
        
        if (_sampleTimer >= SampleInterval)
        {
            _sampleTimer -= SampleInterval;
            RecordSamples(deltaTime, loadedChunks, meshQueue, activeLights, velocity);
        }
    }

    private void RecordSamples(double deltaTime, int loadedChunks, int meshQueue, int activeLights, float velocity)
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
        LoadedChunks.AddSample(loadedChunks);

        // Rendering/Mesh
        MeshQueue.AddSample(meshQueue);
        
        // Active Lights
        ActiveLights.AddSample(activeLights);

        // Velocity
        Velocity.AddSample(velocity);
    }
}
