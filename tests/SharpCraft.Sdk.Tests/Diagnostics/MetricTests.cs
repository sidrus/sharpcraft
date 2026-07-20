using AwesomeAssertions;
using SharpCraft.Sdk.Diagnostics;

namespace SharpCraft.Sdk.Tests.Diagnostics;

public class MetricTests
{
    [Fact]
    public void Latest_WithNoSamples_ShouldReturnZero()
    {
        var metric = new Metric("fps", 4);

        metric.Latest.Should().Be(0);
    }

    [Fact]
    public void Average_WithNoSamples_ShouldReturnZero()
    {
        var metric = new Metric("fps", 4);

        metric.Average.Should().Be(0);
    }

    [Fact]
    public void Max_WithNoSamples_ShouldReturnZero()
    {
        var metric = new Metric("fps", 4);

        metric.Max.Should().Be(0);
    }

    [Fact]
    public void Min_WithNoSamples_ShouldReturnZero()
    {
        var metric = new Metric("fps", 4);

        metric.Min.Should().Be(0);
    }

    [Fact]
    public void Latest_WithSamples_ShouldReturnMostRecentSample()
    {
        var metric = new Metric("fps", 4);

        metric.AddSample(10);
        metric.AddSample(20);
        metric.AddSample(30);

        metric.Latest.Should().Be(30);
    }

    [Fact]
    public void Average_WithSamples_ShouldReturnMeanOfSamples()
    {
        var metric = new Metric("fps", 4);

        metric.AddSample(10);
        metric.AddSample(20);
        metric.AddSample(30);

        metric.Average.Should().Be(20);
    }

    [Fact]
    public void Max_WithSamples_ShouldReturnLargestSample()
    {
        var metric = new Metric("fps", 4);

        metric.AddSample(10);
        metric.AddSample(30);
        metric.AddSample(20);

        metric.Max.Should().Be(30);
    }

    [Fact]
    public void Min_WithSamples_ShouldReturnSmallestSample()
    {
        var metric = new Metric("fps", 4);

        metric.AddSample(30);
        metric.AddSample(10);
        metric.AddSample(20);

        metric.Min.Should().Be(10);
    }

    [Fact]
    public void Average_WhenBufferWraps_ShouldConsiderOnlyRetainedSamples()
    {
        var metric = new Metric("fps", 3);

        // Overflow the ring buffer; only the last 3 samples (20, 30, 40) remain.
        metric.AddSample(10);
        metric.AddSample(20);
        metric.AddSample(30);
        metric.AddSample(40);

        metric.Average.Should().Be(30);
        metric.Min.Should().Be(20);
        metric.Max.Should().Be(40);
        metric.Latest.Should().Be(40);
    }

    [Fact]
    public void GetSamples_WhenBufferWraps_ShouldReturnRetainedSamplesInChronologicalOrder()
    {
        var metric = new Metric("fps", 3);

        metric.AddSample(10);
        metric.AddSample(20);
        metric.AddSample(30);
        metric.AddSample(40);

        metric.GetSamples().Should().Equal(20, 30, 40);
    }
}