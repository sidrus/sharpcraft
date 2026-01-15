using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Sdk.Diagnostics;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.Sdk.UI;

/// <summary>
/// Provides context data for HUD elements.
/// </summary>
public interface IHudContext
{
    /// <summary>
    /// Gets the SDK instance.
    /// </summary>
    ISharpCraftSdk Sdk { get; }

    /// <summary>
    /// Gets the list of loaded mods.
    /// </summary>
    IEnumerable<IMod> LoadedMods { get; }

    /// <summary>
    /// Gets the user avatar provider.
    /// </summary>
    IAvatarProvider? Avatar { get; }

    /// <summary>
    /// Gets the diagnostic metrics provider.
    /// </summary>
    IDiagnosticsProvider? Diagnostics { get; }

    /// <summary>
    /// Gets the local player.
    /// </summary>
    IPlayer? Player { get; }
}
