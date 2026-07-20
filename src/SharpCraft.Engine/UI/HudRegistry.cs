using SharpCraft.Sdk.UI;

namespace SharpCraft.Engine.UI;

/// <summary>
/// The single store of registered HUD elements. Both compiled <see cref="IHud"/> instances and
/// draw-callback registrations land in one list, so consumers iterate a single collection.
/// </summary>
public class HudRegistry : IHudRegistry
{
    private readonly List<IHud> _huds = [];

    /// <summary>
    /// Gets every registered HUD, in registration order.
    /// </summary>
    public IReadOnlyList<IHud> RegisteredHuds => _huds;

    /// <inheritdoc />
    public void RegisterHud(string name, Action<double> drawAction)
    {
        _huds.Add(new CallbackHud(name, drawAction));
    }

    /// <inheritdoc />
    public void RegisterHud(IHud hud)
    {
        _huds.Add(hud);
    }

    private sealed class CallbackHud(string name, Action<double> drawAction) : IHud
    {
        public string Name { get; } = name;

        public void Draw(double deltaTime, IGui gui, IHudContext context)
        {
            drawAction(deltaTime);
        }

        public void OnAwake() { }

        public void OnUpdate(double deltaTime) { }
    }
}