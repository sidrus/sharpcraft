using AwesomeAssertions;
using SharpCraft.Engine;

namespace SharpCraft.Engine.Tests;

public class ScriptSandboxTests
{
    [Fact]
    public async Task ScriptSandbox_ShouldExecuteBasicScript()
    {
        var sandbox = new ScriptSandbox();
        var code = "1 + 1";

        var result = await sandbox.ExecuteAsync<int>(code);

        result.Should().Be(2);
    }
}
