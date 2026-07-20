using SharpCraft.Sdk.Universe;

namespace SharpCraft.Sdk.Commands;

/// <summary>
/// Registry for slash commands.
/// </summary>
public interface ICommandRegistry
{
    /// <summary>
    /// Gets all registered commands.
    /// </summary>
    IReadOnlyDictionary<string, Action<CommandContext>> All { get; }

    /// <summary>
    /// Registers a command.
    /// </summary>
    /// <param name="name">The name of the command (without the slash).</param>
    /// <param name="handler">The handler for the command.</param>
    void RegisterCommand(string name, Action<CommandContext> handler);

    /// <summary>
    /// Executes a command.
    /// </summary>
    /// <param name="input">The full command input (e.g., "/tp 10 20 30").</param>
    /// <param name="player">The player that invoked the command, if any.</param>
    /// <returns>True if the command was found and executed; otherwise, false.</returns>
    bool ExecuteCommand(string input, IPlayer? player = null);
}