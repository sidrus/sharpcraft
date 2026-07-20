using AwesomeAssertions;
using SharpCraft.Engine;

namespace SharpCraft.Sdk.Tests;

public class RegistryTests
{
    private class TestResource
    {
    }

    [Fact]
    public void Register_WithValidNamespacedId_ShouldSucceed()
    {
        var registry = new Registry<TestResource>();
        var resource = new TestResource();

        registry.Register("mod:test", resource);

        registry.Get("mod:test").Should().Be(resource);
    }

    [Fact]
    public void Register_WithoutNamespace_ShouldThrowArgumentException()
    {
        var registry = new Registry<TestResource>();
        var resource = new TestResource();

        var act = () => registry.Register("test", resource);

        act.Should().Throw<ArgumentException>().WithMessage("*namespace*");
    }

    [Fact]
    public void Register_WithInvalidFormat_ShouldThrowArgumentException()
    {
        var registry = new Registry<TestResource>();
        var resource = new TestResource();

        var act = () => registry.Register("mod:test:extra", resource);

        act.Should().Throw<ArgumentException>();
    }
}