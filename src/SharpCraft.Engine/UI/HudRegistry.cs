using SharpCraft.Sdk.UI;

namespace SharpCraft.Engine.UI;

/// <summary>
/// Simple implementation of IHudRegistry that stores registered HUDs.
/// </summary>
public class HudRegistry : IHudRegistry
{
    private readonly List<IHud> _huds = [];
    private readonly List<(string Name, Action<double> DrawAction)> _callbacks = [];

    public IReadOnlyList<IHud> RegisteredHuds => _huds;
    public IReadOnlyList<(string Name, Action<double> DrawAction)> RegisteredCallbacks => _callbacks;

    public void RegisterHud(string name, Action<double> drawAction)
    {
        _callbacks.Add((name, drawAction));
    }

    public void RegisterHud(IHud hud)
    {
        _huds.Add(hud);
    }
}
