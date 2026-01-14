using SharpCraft.Sdk.Numerics;
using AwesomeAssertions;

namespace SharpCraft.Sdk.Tests.Numerics;

public class Vector2Tests
{
    [Fact]
    public void Constructor_ShouldSetXAndY()
    {
        var vector = new Vector2<int>(1, 2);

        vector.X.Should().Be(1);
        vector.Y.Should().Be(2);
    }

    [Fact]
    public void Equality_ShouldBeTrue_WhenValuesAreSame()
    {
        var v1 = new Vector2<int>(1, 2);
        var v2 = new Vector2<int>(1, 2);

        v1.Equals(v2).Should().BeTrue();
        (v1 == v2).Should().BeTrue();
    }

    [Fact]
    public void Equality_ShouldBeFalse_WhenValuesAreDifferent()
    {
        var v1 = new Vector2<int>(1, 2);
        var v2 = new Vector2<int>(2, 1);

        v1.Equals(v2).Should().BeFalse();
        (v1 != v2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_ShouldBeSame_ForEqualVectors()
    {
        var v1 = new Vector2<int>(1, 2);
        var v2 = new Vector2<int>(1, 2);

        v1.GetHashCode().Should().Be(v2.GetHashCode());
    }
}
