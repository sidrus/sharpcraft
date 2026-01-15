namespace SharpCraft.Sdk.UI;

/// <summary>
/// Registry for HUD elements.
/// </summary>
public interface IHudRegistry
{
    /// <summary>
    /// Registers a HUD element.
    /// </summary>
    /// <param name="name">The name of the HUD element.</param>
    /// <param name="drawAction">The action to draw the HUD element.</param>
    void RegisterHud(string name, Action<double> drawAction);

    /// <summary>
    /// Registers a HUD element.
    /// </summary>
    /// <param name="hud">The HUD element to register.</param>
    void RegisterHud(IHud hud);
}
