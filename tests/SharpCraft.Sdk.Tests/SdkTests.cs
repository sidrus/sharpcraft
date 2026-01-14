using FluentAssertions;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Engine;
using SharpCraft.Engine.Blocks;
using SharpCraft.Engine.Messaging;
using Xunit;

namespace SharpCraft.Sdk.Tests;

public class SdkTests
{
    [Fact]
    public void BlockRegistry_ShouldRegisterAndRetrieveBlock()
    {
        var registry = new BlockRegistry();
        var def = new BlockDefinition("test:stone", "Stone");

        registry.Register(def.Id, def);

        registry.Get("test:stone").Should().Be(def);
    }

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

    [Fact]
    public async Task ScriptSandbox_ShouldExecuteBasicScript()
    {
        var sandbox = new ScriptSandbox();
        var code = "1 + 1";

        var result = await sandbox.ExecuteAsync<int>(code);

        result.Should().Be(2);
    }
}
