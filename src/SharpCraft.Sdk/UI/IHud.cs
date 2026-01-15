using SharpCraft.Sdk.Lifecycle;

namespace SharpCraft.Sdk.UI;

/// <summary>
/// Represents a HUD element that can be drawn to the screen.
/// </summary>
public interface IHud : ILifecycle
{
    /// <summary>
    /// Gets the unique name of the HUD element.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Draws the HUD element.
    /// </summary>
    /// <param name="deltaTime">The time since the last frame.</param>
    /// <param name="gui">The GUI abstraction for drawing.</param>
    /// <param name="context">The HUD context providing access to game data.</param>
    void Draw(double deltaTime, IGui gui, IHudContext context);
}
