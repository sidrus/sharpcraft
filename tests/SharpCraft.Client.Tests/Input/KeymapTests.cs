using AwesomeAssertions;
using SharpCraft.Client.Input;
using Silk.NET.Input;

namespace SharpCraft.Client.Tests.Input;

public class KeymapTests
{
    [Fact]
    public void Handle_BoundKey_ShouldInvokeItsAction()
    {
        var invoked = false;
        var keymap = new Keymap();
        keymap.Bind(Key.F3, () => invoked = true);

        var handled = keymap.Handle(Key.F3);

        handled.Should().BeTrue();
        invoked.Should().BeTrue();
    }

    [Fact]
    public void Handle_UnboundKey_ShouldReturnFalseAndDoNothing()
    {
        var keymap = new Keymap();

        keymap.Handle(Key.F3).Should().BeFalse();
    }

    [Fact]
    public void Handle_KeyReboundToNewAction_ShouldInvokeTheLatest()
    {
        var log = new List<string>();
        var keymap = new Keymap();
        keymap.Bind(Key.F4, () => log.Add("first"));
        keymap.Bind(Key.F4, () => log.Add("second"));

        keymap.Handle(Key.F4);

        log.Should().Equal("second");
    }
}