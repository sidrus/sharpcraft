using AwesomeAssertions;
using NSubstitute;
using SharpCraft.Engine.Rendering;
using SharpCraft.Engine.Rendering.Pipeline;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.Client.Tests.Rendering;

public class RenderPassPipelineTests
{
    [Fact]
    public void Constructor_WhenPassReadsResourceWrittenByEarlierPass_ShouldNotThrow()
    {
        var producer = new FakePass("producer", [], [RenderResource.SceneDepth]);
        var consumer = new FakePass("consumer", [RenderResource.SceneDepth], []);

        var act = () => new RenderPassPipeline([producer, consumer]);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WhenPassReadsResourceNotYetWritten_ShouldThrowNamingThePass()
    {
        var consumer = new FakePass("consumer", [RenderResource.SceneDepth], []);
        var producer = new FakePass("producer", [], [RenderResource.SceneDepth]);

        var act = () => new RenderPassPipeline([consumer, producer]);

        act.Should().Throw<RenderPassDependencyException>()
            .Which.PassName.Should().Be("consumer");
    }

    [Fact]
    public void Constructor_WhenPassReadsSeededExternalInput_ShouldNotThrow()
    {
        var consumer = new FakePass("consumer", [RenderResource.InvViewProj], []);
        var externals = new HashSet<RenderResource> { RenderResource.InvViewProj };

        var act = () => new RenderPassPipeline([consumer], externals);

        act.Should().NotThrow();
    }

    [Fact]
    public void Execute_WhenPassEnabled_ShouldRunIt()
    {
        var pass = new FakePass("a", [], []);
        var pipeline = new RenderPassPipeline([pass]);

        pipeline.Execute(Substitute.For<IWorld>(), default, new RenderTargets());

        pass.ExecutedCount.Should().Be(1);
    }

    [Fact]
    public void Execute_WhenPassDisabled_ShouldSkipIt()
    {
        var pass = new FakePass("a", [], []) { IsEnabled = false };
        var pipeline = new RenderPassPipeline([pass]);

        pipeline.Execute(Substitute.For<IWorld>(), default, new RenderTargets());

        pass.ExecutedCount.Should().Be(0);
    }

    [Fact]
    public void Execute_WithMultiplePasses_ShouldRunThemInAuthoredOrder()
    {
        var log = new List<string>();
        var first = new FakePass("first", [], [RenderResource.SceneDepth]) { Log = log };
        var second = new FakePass("second", [RenderResource.SceneDepth], []) { Log = log };
        var pipeline = new RenderPassPipeline([first, second]);

        pipeline.Execute(Substitute.For<IWorld>(), default, new RenderTargets());

        log.Should().ContainInOrder("first", "second");
    }

    [Fact]
    public void Dispose_WithRegisteredPasses_ShouldDisposeEveryPass()
    {
        var pass = new FakePass("a", [], []);
        var pipeline = new RenderPassPipeline([pass]);

        pipeline.Dispose();

        pass.Disposed.Should().BeTrue();
    }

    private sealed class FakePass(string name, RenderResource[] reads, RenderResource[] writes) : IRenderPass
    {
        public string Name => name;
        public IReadOnlyList<RenderResource> Reads => reads;
        public IReadOnlyList<RenderResource> Writes => writes;
        public bool IsEnabled { get; init; } = true;
        public List<string>? Log { get; init; }
        public int ExecutedCount { get; private set; }
        public bool Disposed { get; private set; }

        public bool Enabled(RenderContext context)
        {
            return IsEnabled;
        }

        public void Execute(IWorld world, RenderContext context, RenderTargets targets)
        {
            ExecutedCount++;
            Log?.Add(name);
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
