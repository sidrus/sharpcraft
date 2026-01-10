using System.Numerics;

namespace SharpCraft.Core.Numerics;

public struct Vector2<T>(T x, T y) : IEquatable<Vector2<T>> where T : struct, IEquatable<T>, INumber<T>, IFormattable
{
    public T X = x, Y = y;

    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override bool Equals(object? obj) =>
        obj is Vector2<T> v
        && v.X.Equals(X)
        && v.Y.Equals(Y);

    public static bool operator ==(Vector2<T> left, Vector2<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector2<T> left, Vector2<T> right)
    {
        return !(left == right);
    }

    public bool Equals(Vector2<T> other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y);
    }
}