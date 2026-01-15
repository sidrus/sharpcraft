using System.Numerics;
using Moq;
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
        var mockGui = new Mock<IGui>();
        var mockContext = new Mock<IHudContext>();
        var mockSdk = new Mock<ISharpCraftSdk>();
        var mockCommands = new Mock<ICommandRegistry>();
        var mockChannels = new Mock<IChannelManager>();
        var mockChatChannel = new Mock<IMessageChannel>();
        
        mockContext.Setup(c => c.Sdk).Returns(mockSdk.Object);
        mockSdk.Setup(s => s.Commands).Returns(mockCommands.Object);
        mockSdk.Setup(s => s.Channels).Returns(mockChannels.Object);
        mockChannels.Setup(c => c.GetChannel("chat")).Returns(mockChatChannel.Object);
        
        chatHud.StartTyping("/tp 1 2 3");
        
        // To make Draw call ProcessInput, we need Begin to return true (window open)
        // and InputText to return true (Enter pressed)
        var open = true;
        mockGui.Setup(g => g.Begin(It.IsAny<string>(), ref open, It.IsAny<GuiWindowSettings>())).Returns(true);
        
        mockGui.Setup(g => g.InputText(It.IsAny<string>(), ref It.Ref<string>.IsAny, It.IsAny<uint>(), It.IsAny<GuiInputTextOptions>()))
            .Returns((string label, ref string input, uint max, GuiInputTextOptions flags) => {
                input = "/tp 1 2 3";
                return true;
            });

        // Act
        chatHud.Draw(0, mockGui.Object, mockContext.Object);

        // Assert
        mockCommands.Verify(c => c.ExecuteCommand("/tp 1 2 3", It.IsAny<IPlayer>()), Times.Once);
    }
    [Fact]
    public void Draw_WhenUnknownCommandEntered_ShouldAddErrorMessage()
    {
        // Arrange
        var chatHud = new ChatHud();
        var mockGui = new Mock<IGui>();
        var mockContext = new Mock<IHudContext>();
        var mockSdk = new Mock<ISharpCraftSdk>();
        var mockCommands = new Mock<ICommandRegistry>();
        var mockChannels = new Mock<IChannelManager>();
        var mockChatChannel = new Mock<IMessageChannel>();
        
        mockContext.Setup(c => c.Sdk).Returns(mockSdk.Object);
        mockSdk.Setup(s => s.Commands).Returns(mockCommands.Object);
        mockSdk.Setup(s => s.Channels).Returns(mockChannels.Object);
        mockChannels.Setup(c => c.GetChannel("chat")).Returns(mockChatChannel.Object);
        
        mockCommands.Setup(c => c.ExecuteCommand(It.IsAny<string>(), It.IsAny<IPlayer>())).Returns(false);
        
        chatHud.StartTyping("/unknown");
        
        var open = true;
        mockGui.Setup(g => g.Begin(It.IsAny<string>(), ref open, It.IsAny<GuiWindowSettings>())).Returns(true);
        mockGui.Setup(g => g.BeginChild(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<GuiFrameOptions>(), It.IsAny<GuiWindowSettings>())).Returns(true);
        mockGui.Setup(g => g.InputText(It.IsAny<string>(), ref It.Ref<string>.IsAny, It.IsAny<uint>(), It.IsAny<GuiInputTextOptions>()))
            .Returns((string label, ref string input, uint max, GuiInputTextOptions flags) => {
                input = "/unknown";
                return true;
            });

        // Act
        chatHud.Draw(0, mockGui.Object, mockContext.Object);
        // Draw again to render the message added by ProcessInput
        chatHud.Draw(0, mockGui.Object, mockContext.Object);

        // Assert
        mockGui.Verify(g => g.TextWrapped(It.Is<string>(s => s.Contains("Unknown command: unknown"))), Times.Once);
    }

    [Fact]
    public void Draw_ShouldSubscribeToChatChannel()
    {
        // Arrange
        var chatHud = new ChatHud();
        var mockGui = new Mock<IGui>();
        var mockContext = new Mock<IHudContext>();
        var mockSdk = new Mock<ISharpCraftSdk>();
        var mockChannels = new Mock<IChannelManager>();
        var mockChatChannel = new Mock<IMessageChannel>();
        
        mockContext.Setup(c => c.Sdk).Returns(mockSdk.Object);
        mockSdk.Setup(s => s.Channels).Returns(mockChannels.Object);
        mockChannels.Setup(c => c.GetChannel("chat")).Returns(mockChatChannel.Object);

        // Act
        chatHud.Draw(0, mockGui.Object, mockContext.Object);

        // Assert
        mockChatChannel.Verify(c => c.Subscribe<ChatMessage>(It.IsAny<Action<ChatMessage>>()), Times.Once);
    }

    [Fact]
    public void ChatHud_ShouldDisplayMessagesFromChannel()
    {
        // Arrange
        var chatHud = new ChatHud();
        var mockGui = new Mock<IGui>();
        var mockContext = new Mock<IHudContext>();
        var mockSdk = new Mock<ISharpCraftSdk>();
        var mockChannels = new Mock<IChannelManager>();
        var mockChatChannel = new Mock<IMessageChannel>();
        Action<ChatMessage> messageHandler = null!;
        
        mockContext.Setup(c => c.Sdk).Returns(mockSdk.Object);
        mockSdk.Setup(s => s.Channels).Returns(mockChannels.Object);
        mockChannels.Setup(c => c.GetChannel("chat")).Returns(mockChatChannel.Object);
        
        mockChatChannel.Setup(c => c.Subscribe(It.IsAny<Action<ChatMessage>>()))
            .Callback<Action<ChatMessage>>(h => messageHandler = h)
            .Returns(Mock.Of<IDisposable>());

        var open = true;
        mockGui.Setup(g => g.Begin(It.IsAny<string>(), ref open, It.IsAny<GuiWindowSettings>())).Returns(true);
        mockGui.Setup(g => g.BeginChild(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<GuiFrameOptions>(), It.IsAny<GuiWindowSettings>())).Returns(true);

        // Initial Draw to trigger subscription
        chatHud.Draw(0, mockGui.Object, mockContext.Object);
        
        // Act
        messageHandler.Should().NotBeNull();
        messageHandler(new ChatMessage("Hello from channel!", Vector4.One));
        
        // Second Draw to render messages
        chatHud.Draw(0, mockGui.Object, mockContext.Object);

        // Assert
        mockGui.Verify(g => g.TextWrapped("Hello from channel!"), Times.Once);
    }
}
