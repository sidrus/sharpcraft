using AwesomeAssertions;
using SharpCraft.Engine.Messaging;

namespace SharpCraft.Engine.Tests.Messaging;

public class ChannelManagerTests
{
    [Fact]
    public void Messaging_ShouldPublishAndSubscribe()
    {
        var manager = new ChannelManager();
        var channel = manager.GetChannel("test");
        var received = false;

        channel.Subscribe<string>(msg =>
        {
            msg.Should().Be("hello");
            received = true;
        });

        channel.Publish("hello");

        received.Should().BeTrue();
    }
}
