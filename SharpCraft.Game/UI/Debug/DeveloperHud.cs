using System.Numerics;
using ImGuiNET;
using SharpCraft.Core;
using SharpCraft.Game.Controllers;
using SharpCraft.Game.UI.Components;

namespace SharpCraft.Game.UI.Debug;

public class DeveloperHud : Hud
{
    public override string Name => "DeveloperHud";
    public bool IsVisible { get; set; }
    public event Action? OnVisibilityChanged;

    public override void Draw(double deltaTime, World world, LocalPlayerController? player)
    {
        if (!IsVisible) return;

        ImGui.SetNextWindowPos(new Vector2(10, 300), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(250, 150), ImGuiCond.FirstUseEver);
        
        if (ImGui.Begin("Developer Menu", ref _isVisibleInternal))
        {
            if (player != null)
            {
                Gui.Panel("Cheats", () =>
                {
                    var isFlying = player.IsFlying;
                    if (ImGui.Checkbox("Fly Mode", ref isFlying))
                    {
                        player.IsFlying = isFlying;
                    }
                });
            }
            else
            {
                ImGui.Text("No player controller found.");
            }

            ImGui.End();
        }

        if (!_isVisibleInternal)
        {
            IsVisible = false;
            _isVisibleInternal = true; // reset for next time it's toggled
            OnVisibilityChanged?.Invoke();
        }
    }

    private bool _isVisibleInternal = true;
}
