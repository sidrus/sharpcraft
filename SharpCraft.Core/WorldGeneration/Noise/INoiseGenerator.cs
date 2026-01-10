namespace SharpCraft.Core.WorldGeneration.Noise;

/// <summary>
/// Defines an interface for a 2D noise generator.
/// </summary>
public interface INoiseGenerator
{
    /// <summary>
    /// Evaluates the noise value at the given coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <returns>A noise value, typically in the range [-1, 1].</returns>
    public float Evaluate(float x, float y);
}