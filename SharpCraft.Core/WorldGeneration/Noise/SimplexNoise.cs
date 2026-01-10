using SharpCraft.Core.Numerics;

namespace SharpCraft.Core.WorldGeneration.Noise;

public class SimplexNoise : INoiseGenerator
{
    private readonly int[] _perm;

    public SimplexNoise(int seed = 12345)
    {
        var random = new Random(seed);
        _perm = new int[512];
        var p = Enumerable.Range(0, 256)
            .OrderBy(x => random.Next())
            .ToArray();

        for(var i = 0; i < 512; i++)
        {
            _perm[i] = p[i % 255];
        }
    }

    public float Evaluate(float x, float y)
    {
        const float F2 = 0.366025403f; // (Math.Sqrt(3) - 1) / 2
        const float G2 = 0.211324865f; // (3 - Math.Sqrt(3)) / 6

        var s = (x + y) * F2;
        var i = MathUtils.FastFloor(x + s);
        var j = MathUtils.FastFloor(y + s);

        var t = (i + j) * G2;
        var X0 = i - t;
        var Y0 = j - t;
        var x0 = x - X0;
        var y0 = y - Y0;

        int i1, j1;
        if (x0 > y0) { i1 = 1; j1 = 0; }
        else { i1 = 0; j1 = 1; }

        var x1 = x0 - i1 + G2;
        var y1 = y0 - j1 + G2;
        var x2 = x0 - 1f + 2f * G2;
        var y2 = y0 - 1f + 2f * G2;

        var ii = i & 255;
        var jj = j & 255;
        var gi0 = _perm[ii + _perm[jj]] % 12;
        var gi1 = _perm[ii + i1 + _perm[jj + j1]] % 12;
        var gi2 = _perm[ii + 1 + _perm[jj + 1]] % 12;

        var t0 = 0.5f - x0 * x0 - y0 * y0;
        var n0 = t0 < 0 ? 0 : t0 * t0 * t0 * t0 * MathUtils.Dot(gi0, x0, y0);

        var t1 = 0.5f - x1 * x1 - y1 * y1;
        var n1 = t1 < 0 ? 0 : t1 * t1 * t1 * t1 * MathUtils.Dot(gi1, x1, y1);

        var t2 = 0.5f - x2 * x2 - y2 * y2;
        var n2 = t2 < 0 ? 0 : t2 * t2 * t2 * t2 * MathUtils.Dot(gi2, x2, y2);

        return 70f * (n0 + n1 + n2);
    }
}