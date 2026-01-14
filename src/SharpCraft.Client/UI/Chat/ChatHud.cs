using System.Numerics;
using ImGuiNET;
using SharpCraft.Client.Controllers;
using SharpCraft.Core;

namespace SharpCraft.Client.UI.Chat;

public record ChatMessage(string Text, Vector4 Color);

public class ChatHud : Hud
{
    public override string Name => "ChatHud";
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

    public override void Draw(double deltaTime, HudContext context)
    {
        var windowFlags = ImGuiWindowFlags.NoTitleBar |
                         ImGuiWindowFlags.NoNav | 
                         ImGuiWindowFlags.NoMove | 
                         ImGuiWindowFlags.NoSavedSettings;

        if (!_isTyping)
        {
            windowFlags |= ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoBackground;
        }

        ImGui.SetNextWindowPos(new Vector2(10, ImGui.GetIO().DisplaySize.Y - 220), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(400, 200), ImGuiCond.Always);

        if (ImGui.Begin("ChatWindow", windowFlags))
        {
            // Chat History
            var historyHeight = _isTyping ? -30 : 0;
            if (ImGui.BeginChild("ChatHistory", new Vector2(ImGui.GetContentRegionAvail().X, historyHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoNav))
            {
                foreach (var msg in _messages)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, msg.Color);
                    ImGui.TextWrapped(msg.Text);
                    ImGui.PopStyleColor();
                }

                if (_shouldScrollToBottom)
                {
                    ImGui.SetScrollHereY(1.0f);
                    _shouldScrollToBottom = false;
                }
            }
            ImGui.EndChild();

            if (_isTyping)
            {
                ImGui.Separator();
                if (_focusInput)
                {
                    ImGui.SetKeyboardFocusHere();
                    _focusInput = false;
                }

                ImGui.PushItemWidth(-1);
                if (ImGui.InputText("##ChatInput", ref _inputBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    ProcessInput(context.Player);
                }
                ImGui.PopItemWidth();

                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                {
                    _inputBuffer = string.Empty;
                    IsTyping = false;
                }
            }
        }
        ImGui.End();
    }

    private void ProcessInput(LocalPlayerController? player)
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

    private void ExecuteCommand(string commandLine, LocalPlayerController? player)
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

    private void HandleTeleport(string[] args, LocalPlayerController? player)
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
}
