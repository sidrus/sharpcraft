using SharpCraft.Client.UI.Chat;
using SharpCraft.CoreMods.UI;
using AwesomeAssertions;

namespace SharpCraft.Client.Tests.UI;

public class HudVisibilityTests
{
    [Fact]
    public void ChatHud_SettingIsTyping_ShouldTriggerEvent()
    {
        var hud = new ChatHud();
        var triggered = false;
        hud.OnVisibilityChanged += () => triggered = true;

        hud.IsTyping = true;
        triggered.Should().BeTrue();

        triggered = false;
        hud.IsTyping = false;
        triggered.Should().BeTrue();
    }

    [Fact]
    public void DeveloperHud_SettingIsVisible_ShouldTriggerEvent()
    {
        var hud = new DeveloperHud();
        var triggered = false;
        hud.OnVisibilityChanged += () => triggered = true;

        hud.IsVisible = true;
        triggered.Should().BeTrue();

        triggered = false;
        hud.IsVisible = false;
        triggered.Should().BeTrue();
    }

    [Fact]
    public void GraphicsSettingsHud_SettingIsVisible_ShouldTriggerEvent()
    {
        var hud = new GraphicsSettingsHud();
        var triggered = false;
        hud.OnVisibilityChanged += () => triggered = true;

        hud.IsVisible = true;
        triggered.Should().BeTrue();

        triggered = false;
        hud.IsVisible = false;
        triggered.Should().BeTrue();
    }
}
