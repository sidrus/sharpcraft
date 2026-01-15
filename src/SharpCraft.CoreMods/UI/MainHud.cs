using System.Numerics;
using SharpCraft.Sdk.UI;

namespace SharpCraft.CoreMods.UI;

/// <summary>
/// The main HUD showing the crosshair and player information.
/// </summary>
public class MainHud : IHud
{
    public string Name => "MainHud";

    public void Draw(double deltaTime, IGui gui, IHudContext context)
    {
        var center = gui.GetMainViewportCenter();

        DrawCrosshair(gui, center);
        DrawSteamInfo(gui, context);
    }

    private static void DrawCrosshair(IGui gui, Vector2 position)
    {
        const float size = 10f;
        const float thickness = 2f;
        var color = new Vector4(1, 1, 1, 1);

        // Vertical line
        gui.DrawLine(
            position with { Y = position.Y - size },
            position with { Y = position.Y + size },
            color,
            thickness
        );

        // Horizontal line
        gui.DrawLine(
            position with { X = position.X - size },
            position with { X = position.X + size },
            color,
            thickness
        );
    }

    private static void DrawSteamInfo(IGui gui, IHudContext context)
    {
        var avatar = context.Avatar;
        if (avatar == null || !avatar.IsValid) return;

        var viewportSize = gui.GetMainViewportSize();
        var padding = 10f;
        var pos = new Vector2(viewportSize.X - padding, padding);

        gui.SetNextWindowPos(pos, GuiCond.Always, new Vector2(1f, 0f));
        bool open = true;
        if (gui.Begin("SteamInfo", ref open, GuiWindowSettings.NoDecoration | GuiWindowSettings.AlwaysAutoResize | GuiWindowSettings.NoInputs))
        {
            if (avatar.AvatarTextureId.HasValue)
            {
                // Draw the avatar image (64x64)
                gui.DrawImage(avatar.AvatarTextureId.Value, new Vector2(64, 64));
                gui.SameLine();
            }

            gui.Text(avatar.Name);
            gui.End();
        }
    }

    public void OnAwake() { }
    public void OnUpdate(double deltaTime) { }
}
