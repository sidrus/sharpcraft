using System.Numerics;
using ImGuiNET;
using SharpCraft.Core;
using SharpCraft.Game.Controllers;
using SharpCraft.Game.Integrations.Steam;
using SharpCraft.Game.UI.Components;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Steamworks;

namespace SharpCraft.Game.UI.Main;

public class MainHud(IWindow window, GL gl) : Hud, IDisposable
{
    public override string Name => "MainHud";
    private readonly AvatarLoader _avatarLoader = new(window, gl);

    public async Task LoadSteamAvatar() => await _avatarLoader.LoadSteamAvatar();

    public override void Update(double deltaTime)
    {
        _avatarLoader.Update();
    }

    public override void Draw(double deltaTime, World world, LocalPlayerController? player)
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

    private void DrawSteamInfo()
    {
        if (!SteamClient.IsValid) { return; }

        var viewport = ImGui.GetMainViewport();
        var right = viewport.WorkPos.X + viewport.WorkSize.X - 10;
        var top = viewport.WorkPos.Y + 10;

        ImGui.SetNextWindowPos(new Vector2(right, top), ImGuiCond.Always, new Vector2(1f, 0f));
        ImGui.Begin("SteamInfo", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoInputs);

        if (_avatarLoader.AvatarTexture.HasValue && _avatarLoader.AvatarTexture.Value != 0)
        {
            // Draw the avatar image (64x64)
            ImGui.Image((IntPtr)_avatarLoader.AvatarTexture.Value, new Vector2(64, 64));
            ImGui.SameLine();
        }

        Gui.Label(SteamClient.Name);
        Gui.Property("Steam ID:", SteamClient.SteamId.ToString());
        ImGui.End();
    }

    public void Dispose()
    {
        _avatarLoader.Dispose();
    }
}