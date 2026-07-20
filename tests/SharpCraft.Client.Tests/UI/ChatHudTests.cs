using System.Numerics;
using NSubstitute;
using AwesomeAssertions;
using SharpCraft.Client.UI.Chat;
using SharpCraft.Sdk.UI;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Commands;
using SharpCraft.Sdk.Universe;
using SharpCraft.Sdk.Messaging;

namespace SharpCraft.Client.Tests.UI;

public class ChatHudTests
{
    [Fact]
    public void Draw_WhenCommandEntered_ShouldCallCommandRegistry()
    {
        // Arrange
        var chatHud = new ChatHud();
        var mockGui = Substitute.For<IGui>();
        var mockContext = Substitute.For<IHudContext>();
        var mockSdk = Substitute.For<ISharpCraftSdk>();
        var mockCommands = Substitute.For<ICommandRegistry>();
        var mockChannels = Substitute.For<IChannelManager>();
        var mockChatChannel = Substitute.For<IMessageChannel>();

        mockContext.Sdk.Returns(mockSdk);
        mockSdk.Commands.Returns(mockCommands);
        mockSdk.Channels.Returns(mockChannels);
        mockChannels.GetChannel("chat").Returns(mockChatChannel);

        chatHud.StartTyping("/tp 1 2 3");

        // To make Draw call ProcessInput, we need Begin to return true (window open)
        // and InputText to return true (Enter pressed)
        mockGui.Begin(Arg.Any<string>(), ref Arg.Any<bool>(), Arg.Any<GuiWindowSettings>()).Returns(true);

        mockGui.InputText(Arg.Any<string>(), ref Arg.Any<string>(), Arg.Any<uint>(), Arg.Any<GuiInputTextOptions>())
            .Returns(ci => { ci[1] = "/tp 1 2 3"; return true; });

        // Act
        chatHud.Draw(0, mockGui, mockContext);

        // Assert
        mockCommands.Received(1).ExecuteCommand("/tp 1 2 3", Arg.Any<IPlayer>());
    }
    [Fact]
    public void Draw_WhenUnknownCommandEntered_ShouldAddErrorMessage()
    {
        // Arrange
        var chatHud = new ChatHud();
        var mockGui = Substitute.For<IGui>();
        var mockContext = Substitute.For<IHudContext>();
        var mockSdk = Substitute.For<ISharpCraftSdk>();
        var mockCommands = Substitute.For<ICommandRegistry>();
        var mockChannels = Substitute.For<IChannelManager>();
        var mockChatChannel = Substitute.For<IMessageChannel>();

        mockContext.Sdk.Returns(mockSdk);
        mockSdk.Commands.Returns(mockCommands);
        mockSdk.Channels.Returns(mockChannels);
        mockChannels.GetChannel("chat").Returns(mockChatChannel);

        mockCommands.ExecuteCommand(Arg.Any<string>(), Arg.Any<IPlayer>()).Returns(false);

        chatHud.StartTyping("/unknown");

        mockGui.Begin(Arg.Any<string>(), ref Arg.Any<bool>(), Arg.Any<GuiWindowSettings>()).Returns(true);
        mockGui.BeginChild(Arg.Any<string>(), Arg.Any<Vector2>(), Arg.Any<GuiFrameOptions>(), Arg.Any<GuiWindowSettings>()).Returns(true);
        mockGui.InputText(Arg.Any<string>(), ref Arg.Any<string>(), Arg.Any<uint>(), Arg.Any<GuiInputTextOptions>())
            .Returns(ci => { ci[1] = "/unknown"; return true; });

        // Act
        chatHud.Draw(0, mockGui, mockContext);
        // Draw again to render the message added by ProcessInput
        chatHud.Draw(0, mockGui, mockContext);

        // Assert
        mockGui.Received(1).TextWrapped(Arg.Is<string>(s => s != null && s.Contains("Unknown command: unknown")));
    }

    [Fact]
    public void Draw_ShouldSubscribeToChatChannel()
    {
        // Arrange
        var chatHud = new ChatHud();
        var mockGui = Substitute.For<IGui>();
        var mockContext = Substitute.For<IHudContext>();
        var mockSdk = Substitute.For<ISharpCraftSdk>();
        var mockChannels = Substitute.For<IChannelManager>();
        var mockChatChannel = Substitute.For<IMessageChannel>();

        mockContext.Sdk.Returns(mockSdk);
        mockSdk.Channels.Returns(mockChannels);
        mockChannels.GetChannel("chat").Returns(mockChatChannel);

        // Act
        chatHud.Draw(0, mockGui, mockContext);

        // Assert
        mockChatChannel.Received(1).Subscribe(Arg.Any<Action<ChatMessage>>());
    }

    [Fact]
    public void ChatHud_ShouldDisplayMessagesFromChannel()
    {
        // Arrange
        var chatHud = new ChatHud();
        var mockGui = Substitute.For<IGui>();
        var mockContext = Substitute.For<IHudContext>();
        var mockSdk = Substitute.For<ISharpCraftSdk>();
        var mockChannels = Substitute.For<IChannelManager>();
        var mockChatChannel = Substitute.For<IMessageChannel>();
        Action<ChatMessage> messageHandler = null!;

        mockContext.Sdk.Returns(mockSdk);
        mockSdk.Channels.Returns(mockChannels);
        mockChannels.GetChannel("chat").Returns(mockChatChannel);

        mockChatChannel.Subscribe(Arg.Do<Action<ChatMessage>>(h => messageHandler = h))
            .Returns(Substitute.For<IDisposable>());

        mockGui.Begin(Arg.Any<string>(), ref Arg.Any<bool>(), Arg.Any<GuiWindowSettings>()).Returns(true);
        mockGui.BeginChild(Arg.Any<string>(), Arg.Any<Vector2>(), Arg.Any<GuiFrameOptions>(), Arg.Any<GuiWindowSettings>()).Returns(true);

        // Initial Draw to trigger subscription
        chatHud.Draw(0, mockGui, mockContext);

        // Act
        messageHandler.Should().NotBeNull();
        messageHandler(new ChatMessage("Hello from channel!", Vector4.One));

        // Second Draw to render messages
        chatHud.Draw(0, mockGui, mockContext);

        // Assert
        mockGui.Received(1).TextWrapped("Hello from channel!");
    }
}
