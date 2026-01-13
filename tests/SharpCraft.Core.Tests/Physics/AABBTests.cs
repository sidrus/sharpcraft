using System.Numerics;
using SharpCraft.Core.Physics;
using AwesomeAssertions;
using Bogus;

namespace SharpCraft.Core.Tests.Physics;

public class AABBTests
{
    private readonly Faker _faker = new();

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
    public void Intersects_ShouldReturnTrue_WhenAABBsOverlap()
    {
        var aabb1 = new AABB(new Vector3(0, 0, 0), new Vector3(2, 2, 2));
        var aabb2 = new AABB(new Vector3(1, 1, 1), new Vector3(3, 3, 3));

        var result = aabb1.Intersects(aabb2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Intersects_ShouldReturnFalse_WhenAABBsDoNotOverlap()
    {
        var aabb1 = new AABB(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        var aabb2 = new AABB(new Vector3(2, 2, 2), new Vector3(3, 3, 3));

        var result = aabb1.Intersects(aabb2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Intersects_ShouldReturnTrue_WhenOneAABBIsInsideAnother()
    {
        var aabb1 = new AABB(new Vector3(0, 0, 0), new Vector3(10, 10, 10));
        var aabb2 = new AABB(new Vector3(2, 2, 2), new Vector3(4, 4, 4));

        var result = aabb1.Intersects(aabb2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Intersects_ShouldReturnFalse_WhenAABBsTouchButDoNotOverlap()
    {
        // Intersects uses < and > (exclusive)
        var aabb1 = new AABB(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        var aabb2 = new AABB(new Vector3(1, 0, 0), new Vector3(2, 1, 1));

        var result = aabb1.Intersects(aabb2);

        result.Should().BeFalse();
    }
}
