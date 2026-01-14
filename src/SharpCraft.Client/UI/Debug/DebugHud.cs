using System.Numerics;
using ImGuiNET;
using SharpCraft.Client.Controllers;
using SharpCraft.Client.UI.Components;
using SharpCraft.Client.UI.Debug.Diagnostics;
using SharpCraft.Core;
using SharpCraft.Core.Numerics;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace SharpCraft.Client.UI.Debug;

public class DebugHud : Hud
{
    public override string Name => "DebugHud";
    private readonly DiagnosticsManager _diagnostics = new();
    private int _timeRangeIndex = 1; // Default to 1m
    private readonly string[] _timeRangeLabels = ["30s", "1m", "5m"];
    private readonly int[] _timeRangeSamples = [300, 600, 3000];

    public override void Draw(double deltaTime, HudContext context)
    {
        if (context is { MeshManager: not null, Lighting: not null })
        {
            _diagnostics.Update(deltaTime, context.World, context.MeshManager, context.Lighting);
        }

        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(400, 600), ImGuiCond.FirstUseEver);
        
        if (ImGui.Begin("Diagnostics HUD", ImGuiWindowFlags.None))
        {
            ImGui.SetWindowFontScale(1.2f);
            DrawTimeRangeSelector();
            
            if (ImGui.BeginTabBar("DiagnosticsTabs"))
            {
                if (ImGui.BeginTabItem("General"))
                {
                    DrawPerformanceTab();
                    DrawPlayerTab(context.Player);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("World"))
                {
                    DrawWorldTab();
                    DrawEnvironmentTab(context.Player);
                    ImGui.EndTabItem();
                }
                
                ImGui.EndTabBar();
            }
        }
        ImGui.End();
    }

    private void DrawTimeRangeSelector()
    {
        ImGui.Text("Time Range:");
        ImGui.SameLine();
        ImGui.PushItemWidth(100);
        ImGui.Combo("##timeRange", ref _timeRangeIndex, _timeRangeLabels, _timeRangeLabels.Length);
        ImGui.PopItemWidth();
        ImGui.Separator();
    }

    private void DrawPerformanceTab()
    {
        DrawMetricGraph(_diagnostics.Fps, "FPS", "F1", 0, 200);
        DrawMetricGraph(_diagnostics.CpuUsage, "CPU %", "F1", 0, 100);
        DrawMetricGraph(_diagnostics.RamUsage, "RAM (MB)", "F0", 0, 4096);
        DrawMetricGraph(_diagnostics.GcMemory, "GC Mem (MB)", "F0", 0, 4096);
    }

    private void DrawWorldTab()
    {
        DrawMetricGraph(_diagnostics.LoadedChunks, "Loaded Chunks", "F0", 0, 1000);
        DrawMetricGraph(_diagnostics.MeshQueue, "Mesh Queue", "F0", 0, 100);
        DrawMetricGraph(_diagnostics.ActiveLights, "Active Lights", "F0", 0, 50);
    }

    private void DrawMetricGraph(Metric metric, string label, string format, float min, float max)
    {
        var allSamples = metric.GetSamples();
        var count = _timeRangeSamples[_timeRangeIndex];
        
        float[] displaySamples;
        if (allSamples.Length <= count)
        {
            displaySamples = allSamples;
        }
        else
        {
            displaySamples = allSamples[^count..];
        }

        var latest = metric.Latest;
        var avg = displaySamples.Length > 0 ? displaySamples.Average() : 0;
        
        ImGui.Text($"{label}: {latest.ToString(format)} (avg: {avg.ToString(format)})");
        if (displaySamples.Length > 0)
        {
            ImGui.PlotLines($"##{label}", ref displaySamples[0], displaySamples.Length, 0, null, min, max, new Vector2(0, 60));
        }
        ImGui.Spacing();
    }

    private static void DrawPlayerTab(LocalPlayerController? player)
    {
        if (player == null) return;
        
        Gui.Panel("Player", () => {
            var pos = player.Entity.Position;

            Gui.Property("Position", $"{pos.X:F1}, {pos.Y:F1}, {pos.Z:F1}");
            Gui.Property("Facing", $"{player.Heading} ({player.NormalizedYaw:F1})");
            Gui.Property("Pitch", $"{player.Pitch:F1}");

            Gui.Property("Is Grounded", $"[{player.IsGrounded}]", color: new Vector4(0, 0.5f, 1, 1));
            Gui.Property("Is Flying", $"[{player.IsFlying}]", color: new Vector4(0, 0.5f, 1, 1));

            var underwater = player.IsUnderwater ? "[Underwater]" : string.Empty;
            Gui.Property("Is Swimming", $"[{player.IsSwimming}]", color: new Vector4(0, 0.5f, 1, 1));
            Gui.Label($"{underwater}", visible: player.IsUnderwater, color: new Vector4(1, 0.5f, 0.5f, 1));
        });
    }

    private static void DrawEnvironmentTab(LocalPlayerController? player)
    {
        if (player == null) return;

        Gui.Panel("Environment", () => {
            Gui.Property("Standing on", player.BlockBelow.Type.ToString());
            Gui.Property("Friction", player.Friction.ToString("F2"));
            Gui.Property("Is Solid", $"{player.BlockBelow.IsSolid}");
        });
    }
}