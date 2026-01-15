namespace SharpCraft.Sdk.UI;

/// <summary>
/// Represents a HUD element that can be toggled and interacts with the cursor.
/// </summary>
public interface IInteractiveHud : IHud
{
    /// <summary>
    /// Gets or sets whether the HUD is currently visible and interacting with the user.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Occurs when the visibility of the HUD changes.
    /// </summary>
    event Action? OnVisibilityChanged;
}
