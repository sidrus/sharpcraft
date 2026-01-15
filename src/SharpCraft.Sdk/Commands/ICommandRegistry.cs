namespace SharpCraft.Sdk.Commands;

/// <summary>
/// Context in which a command is executed.
/// </summary>
/// <param name="Caller">The name of the entity that invoked the command.</param>
/// <param name="Args">The arguments passed to the command.</param>
public record CommandContext(string Caller, string[] Args);

/// <summary>
/// Registry for slash commands.
/// </summary>
public interface ICommandRegistry
{
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
    /// <returns>True if the command was found and executed; otherwise, false.</returns>
    bool ExecuteCommand(string input);
}
