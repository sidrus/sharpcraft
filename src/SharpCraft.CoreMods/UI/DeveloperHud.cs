using System.Numerics;
using SharpCraft.Sdk.UI;

namespace SharpCraft.CoreMods.UI;

public class DeveloperHud : IInteractiveHud
{
    public string Name => "DeveloperHud";
    private bool _isVisible;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value) return;
            _isVisible = value;
            OnVisibilityChanged?.Invoke();
        }
    }
    public event Action? OnVisibilityChanged;

    public void Draw(double deltaTime, IGui gui, IHudContext context)
    {
        if (!IsVisible) return;

        var player = context.Player;

        gui.SetNextWindowPos(new Vector2(10, 300), GuiCond.FirstUseEver);
        gui.SetNextWindowSize(new Vector2(250, 150), GuiCond.FirstUseEver);

        var visible = IsVisible;
        if (gui.Begin("Developer Menu", ref visible))
        {
            if (player != null)
            {
                gui.Panel("Cheats", () =>
                {
                    var isFlying = player.IsFlying;
                    gui.Checkbox("Fly Mode", ref isFlying);
                    player.IsFlying = isFlying;

                    var useDevSpeedBoost = player.UseDevSpeedBoost;
                    gui.Checkbox("Speed Boost", ref useDevSpeedBoost);
                    player.UseDevSpeedBoost = useDevSpeedBoost;
                });
            }
            else
            {
                gui.Text("No player controller found.");
            }

            gui.End();
        }

        if (IsVisible != visible)
        {
            IsVisible = visible;
        }
    }

    public void OnAwake() { }
    public void OnUpdate(double deltaTime) { }
}