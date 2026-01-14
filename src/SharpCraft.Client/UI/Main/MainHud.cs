using System.Numerics;
using ImGuiNET;
using SharpCraft.Client.Integrations.Steam;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace SharpCraft.Client.UI.Main;

public partial class MainHud(IWindow window, GL gl) : Hud, IDisposable
{
    public override string Name => "MainHud";
    private readonly AvatarLoader _avatarLoader = new(window, gl);

    public async Task LoadSteamAvatar() => await _avatarLoader.LoadSteamAvatar();

    public override void Draw(double deltaTime, HudContext context)
    {
        var viewport = ImGui.GetMainViewport();
        var center = viewport.GetCenter();

        DrawCrosshair(center);
        DrawSteamInfo();
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



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _avatarLoader.Dispose();
            }

            _disposed = true;
        }
    }

    private bool _disposed;
}