using System.Numerics;

namespace SharpCraft.Core.Numerics;

public static class MathUtils
{
    private static readonly Vector2[] Gradients =
    [
        new(1,1), new(-1,1), new(1,-1), new(-1,-1),
        new(1,0), new(-1,0), new(1,0), new(-1,0),
        new(0,1), new(0,-1), new(0,1), new(0,-1)
    ];

    public static int FastFloor(float x) => (int)x - (x < (int)x ? 1 : 0);

    public static float Dot(int g, float x, float y)
    {
        var grad = Gradients[g];
        return grad.X * x + grad.Y * y;
    }

    public static float Lerp(float a, float b, float t) => a + (b - a) * t;
}