using System.Numerics;
using ImGuiNET;
using SharpCraft.Core;
using SharpCraft.Game.Controllers;
using SharpCraft.Game.UI.Components;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace SharpCraft.Game.UI.Debug;

public class DebugHud
{
    public void Draw(double deltaTime, World world, LocalPlayerController? player)
    {
        ImGui.GetIO().FontGlobalScale = 2f;
        ImGui.SetNextWindowPos(new Vector2(10, 10));
        ImGui.Begin("Debug Info", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove);

        // 1. Performance Section
        Gui.Panel("System", () => {
            Gui.Property("FPS", $"{1.0 / deltaTime:F1}");
            Gui.Property("Chunks", world.GetLoadedChunks().Count().ToString());
        });

        if (player != null)
        {
            Gui.Panel("Player", () => {
                var pos = player.Entity.Position;
                Gui.Property("Position", $"{pos.X:F1}, {pos.Y:F1}, {pos.Z:F1}");

                Gui.Property("Is Swimming", $"[{player.IsSwimming}]", color: new Vector4(0, 0.5f, 1, 1));

                var underwater = player.IsUnderwater ? "[Underwater]" : string.Empty;
                Gui.Label($"{underwater}", visible: player.IsUnderwater, color: new Vector4(1, 0.5f, 0.5f, 1));
            });


            Gui.Panel("Environment", () => {
                Gui.Property("Standing on", player.BlockBelow.Type.ToString());
                Gui.Property("Friction", player.Friction.ToString("F2"));
                Gui.Property("Is Solid", $"{player.BlockBelow.IsSolid}");
            });
        }

        ImGui.End();
    }
}