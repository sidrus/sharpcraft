using SharpCraft.Sdk.Universe;

namespace SharpCraft.Sdk.Commands;

/// <summary>
/// Context in which a command is executed.
/// </summary>
/// <param name="Caller">The name of the entity that invoked the command.</param>
/// <param name="Args">The arguments passed to the command.</param>
/// <param name="Player">The player that invoked the command, if any.</param>
public record CommandContext(string Caller, IReadOnlyList<string> Args, IPlayer? Player = null);
