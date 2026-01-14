using System.Numerics;
using SharpCraft.Sdk.Physics;
using AwesomeAssertions;

namespace SharpCraft.Sdk.Tests.Physics;

public class AABBTests
{
    [Fact]
    public void Properties_ShouldReturnExpectedValues()
    {
        var box = new AABB(new Vector3(0, 0, 0), new Vector3(2, 4, 6));

        box.Center.Should().Be(new Vector3(1, 2, 3));
        box.Size.Should().Be(new Vector3(2, 4, 6));
        box.Extents.Should().Be(new Vector3(1, 2, 3));
    }

    [Fact]
    public void FromPositionSize_ShouldCalculateCorrectMinAndMax()
    {
        var position = new Vector3(10, 20, 30);
        var size = new Vector3(2, 4, 6);
        
        var aabb = AABB.FromPositionSize(position, size);

        // Expected Min: (10 - 2/2, 20, 30 - 6/2) = (9, 20, 27)
        // Expected Max: (10 + 2/2, 20 + 4, 30 + 6/2) = (11, 24, 33)
        aabb.Min.Should().Be(new Vector3(9, 20, 27));
        aabb.Max.Should().Be(new Vector3(11, 24, 33));
    }

    [Fact]
    public void Intersects_ShouldReturnTrue_WhenOverlapping()
    {
        var box1 = new AABB(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        var box2 = new AABB(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1.5f, 1.5f, 1.5f));

        box1.Intersects(box2).Should().BeTrue();
    }

    [Fact]
    public void Intersects_ShouldReturnFalse_WhenNotOverlapping()
    {
        var box1 = new AABB(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        var box2 = new AABB(new Vector3(2, 2, 2), new Vector3(3, 3, 3));

        box1.Intersects(box2).Should().BeFalse();
    }

    [Fact]
    public void Intersects_ShouldReturnTrue_WhenOneInsideOther()
    {
        var box1 = new AABB(new Vector3(0, 0, 0), new Vector3(5, 5, 5));
        var box2 = new AABB(new Vector3(1, 1, 1), new Vector3(2, 2, 2));

        box1.Intersects(box2).Should().BeTrue();
    }

    [Fact]
    public void Intersects_ShouldReturnFalse_WhenAABBsTouchButDoNotOverlap()
    {
        // Intersects uses < and > (exclusive)
        var aabb1 = new AABB(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        var aabb2 = new AABB(new Vector3(1, 0, 0), new Vector3(2, 1, 1));

        aabb1.Intersects(aabb2).Should().BeFalse();
    }
}
