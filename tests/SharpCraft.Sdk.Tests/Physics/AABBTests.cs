using System.Numerics;
using SharpCraft.Sdk.Physics;
using AwesomeAssertions;

namespace SharpCraft.Sdk.Tests.Physics;

public class AABBTests
{
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
}
