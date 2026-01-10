using System.Numerics;

namespace SharpCraft.Core.Numerics;

/// <summary>
/// Represents a 2D vector with generic numeric components.
/// </summary>
/// <typeparam name="T">The numeric type of the components.</typeparam>
public struct Vector2<T>(T x, T y) : IEquatable<Vector2<T>> where T : struct, IEquatable<T>, INumber<T>, IFormattable
{
    /// <summary>
    /// The X component of the vector.
    /// </summary>
    public T X = x;

    /// <summary>
    /// The Y component of the vector.
    /// </summary>
    public T Y = y;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y);


    /// <summary>
    /// Compares two vectors for equality.
    /// </summary>
    public static bool operator ==(Vector2<T> left, Vector2<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two vectors for inequality.
    /// </summary>
    public static bool operator !=(Vector2<T> left, Vector2<T> right)
    {
        return !(left == right);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is Vector2<T> v
        && v.X.Equals(X)
        && v.Y.Equals(Y);

    /// <inheritdoc />
    public bool Equals(Vector2<T> other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y);
    }
}