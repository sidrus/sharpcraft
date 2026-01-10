using System.Numerics;
using ImGuiNET;
using SharpCraft.Game.UI.Components;
using Steamworks;

namespace SharpCraft.Game.UI.Main;

public partial class MainHud
{
    private void DrawSteamInfo()
    {
        if (!SteamClient.IsValid) { return; }

        var pos = Layout.GetPosition(Layout.Anchor.TopRight, padding: 10);

        ImGui.SetNextWindowPos(pos, ImGuiCond.Always, new Vector2(1f, 0f));
        ImGui.Begin("SteamInfo", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoInputs);

        if (_avatarLoader.AvatarTexture.HasValue && _avatarLoader.AvatarTexture.Value != 0)
        {
            // Draw the avatar image (64x64)
            ImGui.Image((IntPtr)_avatarLoader.AvatarTexture.Value, new Vector2(64, 64));
            ImGui.SameLine();
        }

        Gui.Label(SteamClient.Name);
        ImGui.End();
    }
}