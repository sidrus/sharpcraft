using SharpCraft.Sdk.UI;
using System.Numerics;

namespace SharpCraft.CoreMods.UI;

public class DeveloperHud : InteractiveHud
{
    public override string Name => "DeveloperHud";

    public override void Draw(double deltaTime, IGui gui, IHudContext context)
    {
        if (!IsVisible)
        {
            return;
        }

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
}