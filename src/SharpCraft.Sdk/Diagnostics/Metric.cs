namespace SharpCraft.Sdk.Diagnostics;

/// <summary>
/// Represents a metric that collects samples over time.
/// </summary>
public class Metric(string name, int maxSamples)
{
    public string Name { get; } = name;
    private readonly float[] _samples = new float[maxSamples];
    private int _head;
    private int _count;

    public void AddSample(float value)
    {
        _samples[_head] = value;
        _head = (_head + 1) % maxSamples;
        if (_count < maxSamples) _count++;
    }

    public float[] GetSamples()
    {
        var result = new float[_count];
        for (var i = 0; i < _count; i++)
        {
            result[i] = _samples[(_head - _count + i + maxSamples) % maxSamples];
        }
        return result;
    }
    
    public float Latest => _count > 0 ? _samples[(_head - 1 + maxSamples) % maxSamples] : 0;
    public float Average => _count > 0 ? GetSamples().Average() : 0;
    public float Max => _count > 0 ? GetSamples().Max() : 0;
    public float Min => _count > 0 ? GetSamples().Min() : 0;
}
