using SharpCraft.Core.WorldGeneration.Noise;
using AwesomeAssertions;

namespace SharpCraft.Core.Tests.WorldGeneration;

public class SimplexNoiseTests
{
    [Fact]
    public void Evaluate_ShouldReturnSameValue_ForSameInputAndSeed()
    {
        var noise1 = new SimplexNoise(12345);
        var noise2 = new SimplexNoise(12345);
        var x = 1.23f;
        var y = 4.56f;

        var val1 = noise1.Evaluate(x, y);
        var val2 = noise2.Evaluate(x, y);

        val1.Should().Be(val2);
    }

    [Fact]
    public void Evaluate_ShouldReturnDifferentValue_ForDifferentSeed()
    {
        var noise1 = new SimplexNoise(12345);
        var noise2 = new SimplexNoise(54321);
        var x = 1.23f;
        var y = 4.56f;

        var val1 = noise1.Evaluate(x, y);
        var val2 = noise2.Evaluate(x, y);

        val1.Should().NotBe(val2);
    }

    [Fact]
    public void Evaluate_ShouldReturnDifferentValue_ForDifferentInput()
    {
        var noise = new SimplexNoise(12345);
        var x1 = 1.23f;
        var y1 = 4.56f;
        var x2 = 7.89f;
        var y2 = 0.12f;

        var val1 = noise.Evaluate(x1, y1);
        var val2 = noise.Evaluate(x2, y2);

        val1.Should().NotBe(val2);
    }

    [Fact]
    public void Evaluate_ShouldBeWithinExpectedRange()
    {
        var noise = new SimplexNoise(12345);
        var random = new Random(42);

        for (var i = 0; i < 1000; i++)
        {
            var x = (float)random.NextDouble() * 1000 - 500;
            var y = (float)random.NextDouble() * 1000 - 500;
            var val = noise.Evaluate(x, y);

            val.Should().BeInRange(-1.1f, 1.1f);
        }
    }
}
