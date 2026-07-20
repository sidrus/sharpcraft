using SharpCraft.Engine.Rendering.Shaders;
using System.Diagnostics;
using System.Numerics;

namespace SharpCraft.Engine.Rendering;

/// <summary>
/// Histogram-based auto-exposure / eye adaptation (research §5.2). Each frame: build a 256-bin
/// log-luminance histogram of the HDR scene in compute, reduce it to a log-average luminance, pick
/// the exposure that maps the scene's average to middle grey, and adapt toward it over real time.
/// The resulting exposure lives in an SSBO the output pass reads and multiplies before tonemap.
/// </summary>
public sealed class AutoExposure : IDisposable
{
    // Log-luminance window the histogram spans (EV-ish). Scene radiance here sits around order 1.
    private const float MinLogLum = -8.0f;
    private const float MaxLogLum = 4.0f;
    private const float LogLumRange = MaxLogLum - MinLogLum;

    public const uint ExposureBinding = 5;
    private const uint HistogramBinding = 6;

    private readonly GL _gl;
    private readonly ComputeProgram _build;
    private readonly ComputeProgram _average;
    private readonly ShaderStorageBuffer _exposure;   // binding 5: single adapted float (persists)
    private readonly ShaderStorageBuffer _histogram;  // binding 6: 256 bins
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _lastSeconds;
    private bool _disposed;

    public AutoExposure(GL gl)
    {
        _gl = gl;
        _build = new ComputeProgram(gl, Shaders.Shaders.HistogramBuildCompute);
        _average = new ComputeProgram(gl, Shaders.Shaders.HistogramAverageCompute);

        _exposure = new ShaderStorageBuffer(gl, ExposureBinding);
        _exposure.Allocate(sizeof(float));
        _exposure.SetFloat(1.0f); // seed before the first adaptation

        _histogram = new ShaderStorageBuffer(gl, HistogramBinding);
        _histogram.Allocate(256 * sizeof(uint));
        _histogram.Update(new uint[256]); // NamedBufferData leaves contents undefined; zero it
    }

    /// <summary>
    /// Run the histogram + adaptation passes against the rendered HDR colour texture. Call after the
    /// main pass and before the output transform.
    /// </summary>
    public void Update(uint hdrTexture, int width, int height,
        float keyValue, float minExposure, float maxExposure, float adaptationRate)
    {
        double now = _clock.Elapsed.TotalSeconds;
        float dt = (float)Math.Clamp(now - _lastSeconds, 0.0, 0.25);
        _lastSeconds = now;
        float timeCoeff = 1.0f - MathF.Exp(-dt * adaptationRate);

        _histogram.BindBase();
        _exposure.BindBase();

        // Stage 1: build histogram from the HDR frame.
        _build.Use();
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, hdrTexture);
        _build.SetUniform("hdrTexture", 0);
        _build.SetUniform("minLogLum", MinLogLum);
        _build.SetUniform("inverseLogLumRange", 1.0f / LogLumRange);
        _build.SetUniform("dimensions", new Vector2(width, height));
        _build.Dispatch((uint)((width + 15) / 16), (uint)((height + 15) / 16), 1);

        // Stage 2: reduce + adapt.
        _average.Use();
        _average.SetUniform("minLogLum", MinLogLum);
        _average.SetUniform("logLumRange", LogLumRange);
        _average.SetUniform("timeCoeff", timeCoeff);
        _average.SetUniform("pixelCount", (float)(width * height));
        _average.SetUniform("keyValue", keyValue);
        _average.SetUniform("minExposure", minExposure);
        _average.SetUniform("maxExposure", maxExposure);
        _average.Dispatch(1, 1, 1);
    }

    /// <summary>Bind the exposure SSBO for the output pass to read.</summary>
    public void BindForOutput() => _exposure.BindBase();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _build.Dispose();
        _average.Dispose();
        _exposure.Dispose();
        _histogram.Dispose();
        _disposed = true;
    }
}