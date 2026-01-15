using AwesomeAssertions;
using SharpCraft.Engine.Commands;

namespace SharpCraft.Sdk.Tests;

public class CommandTests
{
    [Fact]
    public void RegisterCommand_ShouldExecuteHandler()
    {
        var registry = new CommandRegistry();
        var executed = false;
        string[] receivedArgs = null!;

        registry.RegisterCommand("test", ctx =>
        {
            executed = true;
            receivedArgs = ctx.Args;
        });

        var success = registry.ExecuteCommand("/test arg1 arg2");

        success.Should().BeTrue();
        executed.Should().BeTrue();
        receivedArgs.Should().Equal("arg1", "arg2");
    }

    [Fact]
    public void ExecuteCommand_WithoutSlash_ShouldStillWork()
    {
        var registry = new CommandRegistry();
        var executed = false;

        registry.RegisterCommand("test", _ => executed = true);

        var success = registry.ExecuteCommand("test");

        success.Should().BeTrue();
        executed.Should().BeTrue();
    }

    [Fact]
    public void ExecuteCommand_UnknownCommand_ShouldReturnFalse()
    {
        var registry = new CommandRegistry();

        var success = registry.ExecuteCommand("/unknown");

        success.Should().BeFalse();
    }
}
