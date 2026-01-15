using System.Numerics;
using SharpCraft.Sdk.UI;

namespace SharpCraft.Client.UI.Chat;

public record ChatMessage(string Text, Vector4 Color);

public class ChatHud : IHud
{
    public string Name => "ChatHud";
    private readonly List<ChatMessage> _messages = [];
    private string _inputBuffer = string.Empty;
    private bool _isTyping;
    private bool _focusInput;
    private bool _shouldScrollToBottom;

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
        var windowFlags = GuiWindowSettings.NoTitleBar |
                         GuiWindowSettings.NoSavedSettings;

        if (!_isTyping)
        {
            windowFlags |= GuiWindowSettings.NoInputs;
        }

        var viewportSize = gui.GetMainViewportSize();
        gui.SetNextWindowPos(new Vector2(10, viewportSize.Y - 220), GuiCond.Always);
        gui.SetNextWindowSize(new Vector2(400, 200), GuiCond.Always);

        bool open = true;
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
                    ProcessInput(context.Player);
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

    private void ProcessInput(SharpCraft.Sdk.Universe.IPlayer? player)
    {
        if (string.IsNullOrWhiteSpace(_inputBuffer))
        {
            IsTyping = false;
            return;
        }

        var input = _inputBuffer.Trim();
        AddMessage($"> {input}");

        if (input.StartsWith('/'))
        {
            ExecuteCommand(input[1..], player);
        }

        _inputBuffer = string.Empty;
        IsTyping = false;
    }

    private void ExecuteCommand(string commandLine, SharpCraft.Sdk.Universe.IPlayer? player)
    {
        var parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var command = parts[0].ToLower();
        var args = parts[1..];

        switch (command)
        {
            case "teleport" or "tp":
                HandleTeleport(args, player);
                break;
            default:
                AddMessage($"Unknown command: {command}", new Vector4(1, 0.3f, 0.3f, 1));
                break;
        }
    }

    private void HandleTeleport(string[] args, SharpCraft.Sdk.Universe.IPlayer? player)
    {
        if (player == null) return;

        if (args.Length != 3)
        {
            AddMessage("Usage: /teleport <x> <y> <z>", new Vector4(1, 0.3f, 0.3f, 1));
            return;
        }

        if (float.TryParse(args[0], out var x) &&
            float.TryParse(args[1], out var y) &&
            float.TryParse(args[2], out var z))
        {
            player.Entity.SetPosition(new Vector3(x, y, z));
            AddMessage($"Teleported to {x}, {y}, {z}", new Vector4(0.3f, 1, 0.3f, 1));
        }
        else
        {
            AddMessage("Invalid coordinates", new Vector4(1, 0.3f, 0.3f, 1));
        }
    }

    public void OnAwake() { }
    public void OnUpdate(double deltaTime) { }
}
