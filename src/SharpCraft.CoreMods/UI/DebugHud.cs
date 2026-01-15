using System.Numerics;
using SharpCraft.Sdk.UI;
using SharpCraft.Sdk.Diagnostics;

namespace SharpCraft.CoreMods.UI;

/// <summary>
/// Debug HUD showing performance and world diagnostics.
/// </summary>
public class DebugHud : IHud
{
    public string Name => "DebugHud";

    private const int TimeRangeIndex = 1; // Default to 1m
    private readonly string[] _timeRangeLabels = ["30s", "1m", "5m"];

    public void Draw(double deltaTime, IGui gui, IHudContext context)
    {
        var diagnostics = context.Diagnostics;
        if (diagnostics == null) return;

        gui.SetNextWindowPos(new Vector2(10, 10), GuiCond.FirstUseEver);
        gui.SetNextWindowSize(new Vector2(400, 600), GuiCond.FirstUseEver);
        
        var open = true;
        if (gui.Begin("Diagnostics HUD", ref open))
        {
            gui.SetWindowFontScale(1.2f);
            DrawTimeRangeSelector(gui);
            
            if (gui.BeginTabBar("DiagnosticsTabs"))
            {
                if (gui.BeginTabItem("General"))
                {
                    DrawPerformanceTab(gui, diagnostics);
                    DrawPlayerTab(gui, context.Player);
                    gui.EndTabItem();
                }

                if (gui.BeginTabItem("World"))
                {
                    DrawWorldTab(gui, diagnostics);
                    DrawEnvironmentTab(gui, context.Player);
                    gui.EndTabItem();
                }

                if (gui.BeginTabItem("Mods"))
                {
                    DrawModsTab(gui, context);
                    gui.EndTabItem();
                }
                
                gui.EndTabBar();
            }
        }
        gui.End();
    }

    private void DrawTimeRangeSelector(IGui gui)
    {
        gui.Text("Time Range:");
        gui.SameLine();
        // gui.PushItemWidth(100); // I should add this to IGui if needed, but for now I'll skip or use another way
        // ImGui.Combo... I should add Combo to IGui
        gui.Text(_timeRangeLabels[TimeRangeIndex]); // Simple for now
        gui.Separator();
    }

    private static void DrawPerformanceTab(IGui gui, IDiagnosticsProvider diagnostics)
    {
        DrawMetricInfo(gui, diagnostics.Fps, "FPS", "F1");
        DrawMetricInfo(gui, diagnostics.CpuUsage, "CPU %", "F1");
        DrawMetricInfo(gui, diagnostics.RamUsage, "RAM (MB)", "F0");
        DrawMetricInfo(gui, diagnostics.GcMemory, "GC Mem (MB)", "F0");
    }

    private static void DrawWorldTab(IGui gui, IDiagnosticsProvider diagnostics)
    {
        DrawMetricInfo(gui, diagnostics.LoadedChunks, "Loaded Chunks", "F0");
        DrawMetricInfo(gui, diagnostics.MeshQueue, "Mesh Queue", "F0");
        DrawMetricInfo(gui, diagnostics.ActiveLights, "Active Lights", "F0");
    }

    private static void DrawMetricInfo(IGui gui, Metric metric, string label, string format)
    {
        var latest = metric.Latest;
        var avg = metric.Average;
        
        gui.Text($"{label}: {latest.ToString(format)} (avg: {avg.ToString(format)})");
        // PlotLines not yet in IGui, skipping for now or I can add it
        gui.Spacing();
    }

    private static void DrawPlayerTab(IGui gui, SharpCraft.Sdk.Universe.IPlayer? player)
    {
        if (player == null) return;
        
        gui.Panel("Player", () => {
            var pos = player.Entity.Position;

            gui.Property("Position", string.Format("{0:F1}, {1:F1}, {2:F1}", pos.X, pos.Y, pos.Z));
            gui.Property("Facing", string.Format("{0} ({1:F1})", player.Heading, player.NormalizedYaw));
            gui.Property("Pitch", string.Format("{0:F1}", player.Pitch));

            var velocityMs = player.Entity.Velocity.Length();
            var velocityMph = velocityMs * 2.23694f;
            gui.Property("Velocity", string.Format("{0:F2} m/s ({1:F2} mph)", velocityMs, velocityMph));

            gui.Property("Is Grounded", string.Format("[{0}]", player.IsGrounded), color: new Vector4(0, 0.5f, 1, 1));
            gui.Property("Is Flying", string.Format("[{0}]", player.IsFlying), color: new Vector4(0, 0.5f, 1, 1));

            var underwater = player.IsUnderwater ? "[Underwater]" : string.Empty;
            gui.Property("Is Swimming", string.Format("[{0}]", player.IsSwimming), color: new Vector4(0, 0.5f, 1, 1));
            if (player.IsUnderwater)
            {
                gui.Text(underwater, color: new Vector4(1, 0.5f, 0.5f, 1));
            }
        });
    }

    private static void DrawEnvironmentTab(IGui gui, SharpCraft.Sdk.Universe.IPlayer? player)
    {
        if (player == null) return;

        gui.Panel("Environment", () => {
            // gui.Property("Standing on", ...); // Need way to get block info
            gui.Property("Friction", player.Friction.ToString("F2"));
        });
    }

    private static void DrawModsTab(IGui gui, IHudContext context)
    {
        foreach (var mod in context.LoadedMods.Select(m => m.Manifest))
        {
            if (!gui.CollapsingHeader($"{mod.Name} ({mod.Version})###{mod.Id}"))
            {
                continue;
            }

            gui.Indent();
            gui.Text($"ID: {mod.Id}");
            gui.Text($"Author: {mod.Author}");
            gui.Spacing();

            gui.Text("Resources:");
            DrawResourceCount(gui, "Blocks", context.Sdk.Blocks.All, mod.Id);
            DrawResourceCount(gui, "Assets", context.Sdk.Assets.All, mod.Id);
            DrawCommandCount(gui, context.Sdk.Commands.All, mod.Id);
            DrawResourceCount(gui, "World Generators", context.Sdk.World.All, mod.Id);
                
            gui.Unindent();
            gui.Spacing();
        }
    }

    private static void DrawResourceCount<T>(IGui gui, string label, IEnumerable<KeyValuePair<SharpCraft.Sdk.Resources.ResourceLocation, T>> registry, string modId)
    {
        var count = registry.Count(kv => kv.Key.Namespace == modId);
        if (count > 0)
        {
            gui.Text($"- {label}: {count}");
        }
    }

    private static void DrawCommandCount(IGui gui, IReadOnlyDictionary<string, Action<SharpCraft.Sdk.Commands.CommandContext>> commands, string modId)
    {
        var count = commands.Count(kv => kv.Key.StartsWith($"{modId}:"));
        if (count > 0)
        {
            gui.Text($"- Commands: {count}");
        }
    }

    public void OnAwake() { }
    public void OnUpdate(double deltaTime) { }
}
