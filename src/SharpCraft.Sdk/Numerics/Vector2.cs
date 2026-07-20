using System.Numerics;

namespace SharpCraft.Sdk.Numerics;

/// <summary>
/// Represents a 2D vector with generic numeric components.
/// </summary>
/// <typeparam name="T">The numeric type of the components.</typeparam>
/// <param name="X">The X component.</param>
/// <param name="Y">The Y component.</param>
public readonly record struct Vector2<T>(T X, T Y) where T : struct, INumber<T>;