using System.Numerics;
using ImGuiNET;

namespace SharpCraft.Game.UI;

public static class Layout
{
    public enum Anchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center,
        TopCenter,
        BottomCenter,
    }

    public static Vector2 GetPosition(Anchor? anchor = Anchor.TopLeft, int? padding = 0, Vector2? anchorOffset = null)
    {
        var viewport = ImGui.GetMainViewport();
        anchorOffset ??= Vector2.Zero;

        var position = anchor switch
        {
            Anchor.TopLeft => viewport.WorkPos,
            Anchor.TopRight => viewport.WorkPos + viewport.WorkSize with { Y = 0 },
            Anchor.TopCenter => viewport.WorkPos + new Vector2(viewport.WorkSize.X / 2, 0),
            Anchor.BottomLeft => viewport.WorkPos + viewport.WorkSize with { X = 0 },
            Anchor.BottomRight => viewport.WorkPos + viewport.WorkSize,
            Anchor.BottomCenter => viewport.WorkPos + viewport.WorkSize with { X = viewport.WorkSize.X / 2 },
            Anchor.Center => viewport.WorkPos + viewport.WorkSize / 2,
            _ => viewport.WorkPos
        };

        position += anchorOffset.Value;

        if (!padding.HasValue)
        {
            return position;
        }

        var p = padding.Value;
        var padVector = anchor switch
        {
            Anchor.TopLeft => new Vector2(p, p),
            Anchor.TopRight => new Vector2(-p, p),
            Anchor.BottomLeft => new Vector2(p, -p),
            Anchor.BottomRight => new Vector2(-p, -p),
            Anchor.TopCenter => new Vector2(0, p),
            Anchor.BottomCenter => new Vector2(0, -p),
            Anchor.Center => Vector2.Zero,
            _ => new Vector2(p, p)
        };

        position += padVector;

        return position;
    }
    
}