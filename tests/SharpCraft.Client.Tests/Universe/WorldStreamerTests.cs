using AwesomeAssertions;
using SharpCraft.Client.Universe;
using SharpCraft.Sdk.Numerics;
using System.Numerics;

namespace SharpCraft.Client.Tests.Universe;

public class WorldStreamerTests
{
    private sealed class GenerateSpy
    {
        public int Calls { get; private set; }
        public int LastRenderDistance { get; private set; }
        public Vector3 LastPosition { get; private set; }
        public Task Result { get; set; } = Task.CompletedTask;

        public Task Generate(int renderDistance, Vector3 position)
        {
            Calls++;
            LastRenderDistance = renderDistance;
            LastPosition = position;
            return Result;
        }
    }

    private static Vector3 InChunk(int chunkX, int chunkZ) => new(chunkX * 16f + 1f, 64f, chunkZ * 16f + 1f);

    [Fact]
    public void Update_WhenEnteringNewChunk_ShouldInvokeGenerate()
    {
        var spy = new GenerateSpy();
        var streamer = new WorldStreamer(spy.Generate);

        streamer.Update(InChunk(2, 3), renderDistance: 8);

        spy.Calls.Should().Be(1);
        spy.LastRenderDistance.Should().Be(8);
    }

    [Fact]
    public void Update_WhenStayingInSameChunk_ShouldNotInvokeGenerateAgain()
    {
        var spy = new GenerateSpy();
        var streamer = new WorldStreamer(spy.Generate);

        streamer.Update(InChunk(2, 3), renderDistance: 8);
        streamer.Update(new Vector3(2 * 16f + 9f, 70f, 3 * 16f + 4f), renderDistance: 8);

        spy.Calls.Should().Be(1);
    }

    [Fact]
    public void Update_WhileGenerateStillInFlight_ShouldNotStartAnother()
    {
        var pending = new TaskCompletionSource();
        var spy = new GenerateSpy { Result = pending.Task };
        var streamer = new WorldStreamer(spy.Generate);

        streamer.Update(InChunk(0, 0), renderDistance: 8);
        streamer.Update(InChunk(5, 5), renderDistance: 8);

        spy.Calls.Should().Be(1);
    }

    [Fact]
    public void Update_AfterForceRefresh_ShouldRegenerateEvenInSameChunk()
    {
        var spy = new GenerateSpy();
        var streamer = new WorldStreamer(spy.Generate);

        streamer.Update(InChunk(1, 1), renderDistance: 8);
        streamer.ForceRefresh();
        streamer.Update(InChunk(1, 1), renderDistance: 8);

        spy.Calls.Should().Be(2);
    }
}
