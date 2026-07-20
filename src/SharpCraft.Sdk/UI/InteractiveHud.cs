namespace SharpCraft.Sdk.UI;

/// <summary>
/// Base class for interactive HUDs, providing the common toggleable-visibility behavior: a guarded
/// <see cref="IsVisible"/> setter that raises <see cref="OnVisibilityChanged"/> only on a real change.
/// Subclasses supply <see cref="Name"/> and <see cref="Draw"/>.
/// </summary>
public abstract class InteractiveHud : IInteractiveHud
{
    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public bool IsVisible
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnVisibilityChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public event Action? OnVisibilityChanged;

    /// <inheritdoc />
    public abstract void Draw(double deltaTime, IGui gui, IHudContext context);

    /// <inheritdoc />
    public virtual void OnAwake()
    {
    }

    /// <inheritdoc />
    public virtual void OnUpdate(double deltaTime)
    {
    }
}
