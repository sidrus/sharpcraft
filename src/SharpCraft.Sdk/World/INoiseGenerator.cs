namespace SharpCraft.Sdk.World;

/// <summary>
/// Defines a provider for coherent noise (e.g., Perlin or Simplex).
/// </summary>
public interface INoiseGenerator
{
    /// <summary>
    /// Evaluates the noise value at the given coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <returns>A noise value, typically in the range [-1, 1].</returns>
    float Evaluate(float x, float y);
}
