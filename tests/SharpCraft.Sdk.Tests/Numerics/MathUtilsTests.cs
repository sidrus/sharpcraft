using SharpCraft.Sdk.Numerics;
using AwesomeAssertions;

namespace SharpCraft.Sdk.Tests.Numerics;

public class MathUtilsTests
{
    [Theory]
    [InlineData(0, 10, 20, 30)] // (1,1) -> 1*10 + 1*20 = 30
    [InlineData(4, 10, 20, 10)] // (1,0) -> 1*10 + 0*20 = 10
    [InlineData(9, 10, 20, -20)] // (0,-1) -> 0*10 + (-1)*20 = -20
    public void Dot_ShouldReturnCorrectDotProduct(int g, float x, float y, float expected)
    {
        var result = MathUtils.Dot(g, x, y);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, "North")]
    [InlineData(-45, "North-East")]
    [InlineData(-90, "East")]
    [InlineData(-135, "South-East")]
    [InlineData(-180, "South")]
    [InlineData(-225, "South-West")]
    [InlineData(-270, "West")]
    [InlineData(-315, "North-West")]
    [InlineData(-360, "North")]
    [InlineData(90, "West")]
    [InlineData(180, "South")]
    [InlineData(270, "East")]
    [InlineData(45, "North-West")]
    public void GetHeading_ShouldReturnCorrectDirection(float yaw, string expected)
    {
        var result = MathUtils.GetHeading(yaw);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(90, 0, 0)]
    [InlineData(-90, 0, 0)]
    [InlineData(0, 45, 0)]
    [InlineData(0, -45, 0)]
    [InlineData(45, 45, 0)]
    [InlineData(-45, -45, 0)]
    [InlineData(179, 0, 0)]
    public void ToEulerAngles_ShouldMatchInput_WhenCreatedFromYawPitchRoll(float yaw, float pitch, float roll)
    {
        // Arrange
        var q = System.Numerics.Quaternion.CreateFromYawPitchRoll(yaw * MathF.PI / 180f, pitch * MathF.PI / 180f, roll * MathF.PI / 180f);

        // Act
        var (resultYaw, resultPitch, resultRoll) = MathUtils.ToEulerAngles(q);

        // Assert
        resultYaw.Should().BeApproximately(yaw, 1e-2f);
        resultPitch.Should().BeApproximately(pitch, 1e-2f);
        resultRoll.Should().BeApproximately(roll, 1e-2f);
    }
}
