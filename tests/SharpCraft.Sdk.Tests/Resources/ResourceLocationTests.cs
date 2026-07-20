using AwesomeAssertions;
using SharpCraft.Sdk.Resources;

namespace SharpCraft.Sdk.Tests.Resources;

public class ResourceLocationTests
{
    [Fact]
    public void Parse_WithValidLocation_ShouldSplitNamespaceAndPath()
    {
        var location = ResourceLocation.Parse("minecraft:dirt");

        location.Namespace.Should().Be("minecraft");
        location.Path.Should().Be("dirt");
    }

    [Fact]
    public void Parse_WithoutSeparator_ShouldThrowArgumentException()
    {
        var act = () => ResourceLocation.Parse("dirt");

        act.Should().Throw<ArgumentException>().WithMessage("*namespace:path*");
    }

    [Fact]
    public void Parse_WithTooManySeparators_ShouldThrowArgumentException()
    {
        var act = () => ResourceLocation.Parse("mod:path:extra");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryParse_WithValidLocation_ShouldReturnTrueAndValue()
    {
        var success = ResourceLocation.TryParse("mod:block", out var result);

        success.Should().BeTrue();
        result.Should().Be(new ResourceLocation("mod", "block"));
    }

    [Fact]
    public void TryParse_WithInvalidLocation_ShouldReturnFalseAndNull()
    {
        var success = ResourceLocation.TryParse("no-separator", out var result);

        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void ToString_ShouldRoundTripThroughParse()
    {
        var location = new ResourceLocation("mod", "block");

        ResourceLocation.Parse(location.ToString()).Should().Be(location);
    }

    [Fact]
    public void ImplicitConversionFromString_ShouldParse()
    {
        ResourceLocation location = "mod:block";

        location.Should().Be(new ResourceLocation("mod", "block"));
    }

    [Fact]
    public void ExplicitConversionToString_ShouldFormatNamespaceAndPath()
    {
        var text = (string)new ResourceLocation("mod", "block");

        text.Should().Be("mod:block");
    }

    [Fact]
    public void Equality_ShouldBeValueBased()
    {
        new ResourceLocation("mod", "block")
            .Should().Be(new ResourceLocation("mod", "block"));
    }
}
