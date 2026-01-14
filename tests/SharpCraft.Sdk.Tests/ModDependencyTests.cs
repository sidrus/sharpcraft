using FluentAssertions;
using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Engine.Lifecycle;
using Xunit;

namespace SharpCraft.Sdk.Tests;

public class ModDependencyTests
{
    private class MockMod(string id, params string[] deps) : IMod
    {
        public ModManifest Manifest { get; } = new(id, id, "Author", "1.0.0", deps, []);
        public void OnEnable() { }
        public void OnDisable() { }
    }

    [Fact]
    public void SortByDependencies_ShouldReturnCorrectOrder()
    {
        var modA = new MockMod("A");
        var modB = new MockMod("B", "A");
        var modC = new MockMod("C", "B");

        var sorted = ModLoader.SortByDependencies([modC, modA, modB]);

        sorted.Should().ContainInOrder(modA, modB, modC);
    }

    [Fact]
    public void SortByDependencies_WithMissingDependency_ShouldThrowException()
    {
        var modA = new MockMod("A", "MissingMod");

        Action act = () => ModLoader.SortByDependencies([modA]);

        act.Should().Throw<Exception>().WithMessage("*MissingMod*");
    }

    [Fact]
    public void SortByDependencies_WithCircularDependency_ShouldThrowException()
    {
        var modA = new MockMod("A", "B");
        var modB = new MockMod("B", "A");

        Action act = () => ModLoader.SortByDependencies([modA, modB]);

        act.Should().Throw<Exception>().WithMessage("*circular*");
    }
}
