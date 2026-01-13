using SharpCraft.Core.Numerics;
using AwesomeAssertions;
using Bogus;

namespace SharpCraft.Core.Tests.Numerics;

public class Vector2Tests
{
    private readonly Faker _faker = new();

    [Fact]
    public void Equals_ShouldReturnTrue_ForSameValues()
    {
        var x = _faker.Random.Int();
        var y = _faker.Random.Int();
        var v1 = new Vector2<int>(x, y);
        var v2 = new Vector2<int>(x, y);

        var result = v1.Equals(v2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_ForDifferentValues()
    {
        var v1 = new Vector2<int>(1, 2);
        var v2 = new Vector2<int>(1, 3);

        var result = v1.Equals(v2);

        result.Should().BeFalse();
    }

    [Fact]
    public void OperatorEquality_ShouldReturnTrue_ForSameValues()
    {
        var x = _faker.Random.Int();
        var y = _faker.Random.Int();
        var v1 = new Vector2<int>(x, y);
        var v2 = new Vector2<int>(x, y);

        var result = v1 == v2;

        result.Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_ShouldBeSame_ForSameValues()
    {
        var x = _faker.Random.Int();
        var y = _faker.Random.Int();
        var v1 = new Vector2<int>(x, y);
        var v2 = new Vector2<int>(x, y);

        v1.GetHashCode().Should().Be(v2.GetHashCode());
    }
}
