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
        if (_count < maxSamples)
        {
            _count++;
        }
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

    // Sum/min/max are order-independent, and the live samples always occupy slots
    // [0, _count), so these iterate the backing array directly without allocating.
    public float Average
    {
        get
        {
            if (_count == 0)
            {
                return 0;
            }

            var sum = 0f;
            for (var i = 0; i < _count; i++)
            {
                sum += _samples[i];
            }
            return sum / _count;
        }
    }

    public float Max
    {
        get
        {
            if (_count == 0)
            {
                return 0;
            }

            var max = _samples[0];
            for (var i = 1; i < _count; i++)
            {
                if (_samples[i] > max)
                {
                    max = _samples[i];
                }
            }
            return max;
        }
    }

    public float Min
    {
        get
        {
            if (_count == 0)
            {
                return 0;
            }

            var min = _samples[0];
            for (var i = 1; i < _count; i++)
            {
                if (_samples[i] < min)
                {
                    min = _samples[i];
                }
            }
            return min;
        }
    }
}