namespace SharpCraft.Core.WorldGeneration.Noise;

public interface INoiseGenerator
{
    public float Evaluate(float x, float y);
}