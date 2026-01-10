using System.Numerics;
using ImGuiNET;
using SharpCraft.Core;
using SharpCraft.Game.Controllers;

namespace SharpCraft.Game.UI.Main;

public class MainHud : Hud
{
    public override string Name => "MainHud";

    public override void Draw(double deltaTime, World world, LocalPlayerController? player)
    {
        var viewport = ImGui.GetMainViewport();
        var center = viewport.GetCenter();

        DrawCrosshair(center);
    }

    private static void DrawCrosshair(Vector2 position)
    {
        const float size = 10f;
        const float thickness = 2f;
        var color = ImGui.GetColorU32(new Vector4(1, 1, 1, 1));
        var drawList = ImGui.GetForegroundDrawList();

        // Vertical line
        drawList.AddLine(
            position with { Y = position.Y - size },
            position with { Y = position.Y + size },
            color,
            thickness
        );

        // Horizontal line
        drawList.AddLine(
            position with { X = position.X - size },
            position with { X = position.X + size },
            color,
            thickness
        );
    }
}