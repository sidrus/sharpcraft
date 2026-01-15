using System.Collections.Concurrent;
using SharpCraft.Sdk.Commands;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.Engine.Commands;

/// <summary>
/// Runtime implementation of the command registry.
/// </summary>
public class CommandRegistry : ICommandRegistry
{
    private readonly ConcurrentDictionary<string, Action<CommandContext>> _commands = new();

    public IReadOnlyDictionary<string, Action<CommandContext>> All => _commands;

    public void RegisterCommand(string name, Action<CommandContext> handler)
    {
        name = name.TrimStart('/').ToLower();
        if (!_commands.TryAdd(name, handler))
        {
            throw new ArgumentException($"Command '{name}' is already registered.", nameof(name));
        }
    }

    public bool ExecuteCommand(string input, IPlayer? player = null)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        input = input.TrimStart('/');
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        var commandName = parts[0].ToLower();
        var args = parts.Skip(1).ToArray();

        if (_commands.TryGetValue(commandName, out var handler))
        {
            try
            {
                handler(new CommandContext(player != null ? "Player" : "System", args, player));
                return true;
            }
            catch (Exception)
            {
                // Isolated exception as per specs 3.3
                return false;
            }
        }

        return false;
    }
}
