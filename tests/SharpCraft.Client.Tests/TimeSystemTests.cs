using System;
using AwesomeAssertions;
using Xunit;

using SharpCraft.Engine.Universe;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.Client.Tests;

public class TimeSystemTests
{
    [Theory]
    [InlineData(10f)] // 10 minute day
    [InlineData(20f)] // 20 minute day
    [InlineData(1f)]  // 1 minute day
    public void TimeScale_ShouldCompleteFullCycle_InSpecifiedMinutes(float dayDurationInMinutes)
    {
        // Arrange
        var worldTime = new WorldTime { DayDurationInMinutes = dayDurationInMinutes };
        var secondsInDay = dayDurationInMinutes * 60f;

        // Act
        // Initial angle should be PI (6 AM) based on current implementation of WorldTime
        var angleAtStart = worldTime.SunAngle;
        
        // Advance time by one full day
        worldTime.OnUpdate(secondsInDay);
        var angleAfterDay = worldTime.SunAngle;

        // Assert
        angleAtStart.Should().BeApproximately(MathF.PI, 0.0001f);
        angleAfterDay.Should().BeApproximately(MathF.PI, 0.0001f);
    }

    [Fact]
    public void GameTimeCalculation_ShouldBeCorrect()
    {
        // Arrange
        var worldTime = new WorldTime { DayDurationInMinutes = 10f };
        
        // Assert various points in time
        // Initially at 0s, it's 6 AM
        worldTime.FormattedTime.Should().Be("06:00 AM");

        // 6 AM is PI
        worldTime.SunAngle.Should().BeApproximately(MathF.PI, 0.001f);

        // Advance to 12 PM (6 hours = 0.25 of day)
        // Day is 10 min = 600s. 0.25 of 600s = 150s
        worldTime.OnUpdate(150);
        worldTime.FormattedTime.Should().Be("12:00 PM");
        worldTime.SunAngle.Should().BeApproximately(1.5f * MathF.PI, 0.001f);

        // Advance to 6 PM (another 6 hours)
        worldTime.OnUpdate(150);
        worldTime.FormattedTime.Should().Be("06:00 PM");
        // 2PI wraps to 0 or stays 2PI? Normalized to [0, 2PI] in WorldTime
        worldTime.SunAngle.Should().BeApproximately(0.0f, 0.001f); // 2PI % 2PI = 0

        // Advance to 12 AM (another 6 hours)
        worldTime.OnUpdate(150);
        worldTime.FormattedTime.Should().Be("12:00 AM");
        worldTime.SunAngle.Should().BeApproximately(0.5f * MathF.PI, 0.001f);
    }
}
