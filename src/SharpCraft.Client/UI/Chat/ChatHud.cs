using System.Numerics;
using SharpCraft.Sdk.UI;

namespace SharpCraft.Client.UI.Chat;

public class ChatHud : IInteractiveHud
{
    public string Name => "ChatHud";
    private readonly List<ChatMessage> _messages = [];
    private string _inputBuffer = string.Empty;
    private bool _isTyping;
    private bool _focusInput;
    private bool _shouldScrollToBottom;
    private IDisposable? _chatSubscription;

    public bool IsVisible
    {
        get => IsTyping;
        set => IsTyping = value;
    }

    public bool IsTyping
    {
        get => _isTyping;
        set
        {
            if (_isTyping == value) return;
            _isTyping = value;
            if (_isTyping)
            {
                _focusInput = true;
            }
            OnVisibilityChanged?.Invoke();
        }
    }

    public event Action? OnVisibilityChanged;

    public void StartTyping(string prefix = "")
    {
        _inputBuffer = prefix;
        IsTyping = true;
    }

    public ChatHud()
    {
        AddMessage("Welcome to SharpCraft!", new Vector4(1, 1, 0, 1));
        AddMessage("Type 'T' to chat or '/' to enter a command.");
    }

    public void AddMessage(string text, Vector4? color = null)
    {
        _messages.Add(new ChatMessage(text, color ?? new Vector4(1, 1, 1, 1)));
        _shouldScrollToBottom = true;
    }

    public void Draw(double deltaTime, IGui gui, IHudContext context)
    {
        _chatSubscription ??= context.Sdk.Channels.GetChannel("chat").Subscribe<ChatMessage>(msg => AddMessage(msg.Text, msg.Color));

        var windowFlags = GuiWindowSettings.NoTitleBar |
                         GuiWindowSettings.NoSavedSettings;

        if (!_isTyping)
        {
            windowFlags |= GuiWindowSettings.NoInputs;
        }

        var viewportSize = gui.GetMainViewportSize();
        gui.SetNextWindowPos(new Vector2(10, viewportSize.Y - 220), GuiCond.Always);
        gui.SetNextWindowSize(new Vector2(400, 200), GuiCond.Always);

        var open = true;
        if (gui.Begin("ChatWindow", ref open, windowFlags))
        {
            // Chat History
            var historyHeight = _isTyping ? -30 : 0;
            if (gui.BeginChild("ChatHistory", new Vector2(gui.GetContentRegionAvail().X, historyHeight), GuiFrameOptions.None, GuiWindowSettings.NoSavedSettings))
            {
                foreach (var msg in _messages)
                {
                    gui.PushStyleColor(GuiCol.Text, msg.Color);
                    gui.TextWrapped(msg.Text);
                    gui.PopStyleColor();
                }

                if (_shouldScrollToBottom)
                {
                    gui.SetScrollHereY(1.0f);
                    _shouldScrollToBottom = false;
                }
            }
            gui.EndChild();

            if (_isTyping)
            {
                gui.Separator();
                if (_focusInput)
                {
                    gui.SetKeyboardFocusHere();
                    _focusInput = false;
                }

                if (gui.InputText("##ChatInput", ref _inputBuffer, 256, GuiInputTextOptions.EnterReturnsTrue))
                {
                    ProcessInput(context);
                }

                if (gui.IsKeyPressed(GuiKey.Escape))
                {
                    _inputBuffer = string.Empty;
                    IsTyping = false;
                }
            }
        }
        gui.End();
    }

    private void ProcessInput(IHudContext context)
    {
        if (string.IsNullOrWhiteSpace(_inputBuffer))
        {
            IsTyping = false;
            return;
        }

        var input = _inputBuffer.Trim();
        AddMessage($"> {input}");

        if (input.StartsWith('/') && !context.Sdk.Commands.ExecuteCommand(input, context.Player))
        {
            var commandName = input.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].TrimStart('/');
            AddMessage($"Unknown command: {commandName}", new Vector4(1, 0.3f, 0.3f, 1));
        }

        _inputBuffer = string.Empty;
        IsTyping = false;
    }

    public void OnAwake() { }
    public void OnUpdate(double deltaTime) { }
}
