using System.Numerics;
using SharpCraft.Sdk.UI;

namespace SharpCraft.Client.UI.Debug;

public class DeveloperHud : IHud
{
    public string Name => "DeveloperHud";
    public bool IsVisible { get; set; }
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
                    if (player.IsFlying != isFlying)
                    {
                        player.IsFlying = isFlying;
                    }
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
            OnVisibilityChanged?.Invoke();
        }
    }

    public void OnAwake() { }
    public void OnUpdate(double deltaTime) { }
}
