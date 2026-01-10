using System.Numerics;
using ImGuiNET;
using SharpCraft.Game.UI.Components;

namespace SharpCraft.Game.UI.Settings;

public class GraphicsSettingsHud
{
    public bool IsVisible { get; set; }

    public bool UseNormalMap = true;
    public float NormalStrength = 0.5f;
    public bool UseAoMap = true;
    public float AoMapStrength = 0.5f;
    public bool UseSpecularMap = true;
    public float SpecularMapStrength = 0.5f;

    public float FogNearFactor = 0.3f;
    public float FogFarFactor = 0.95f;

    public event Action? OnVisibilityChanged;

    public void Draw()
    {
        if (!IsVisible) return;

        // Force the window to center
        var io = ImGui.GetIO();
        ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X * 0.5f, io.DisplaySize.Y * 0.5f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(0, 0)); // Auto-height

        // Use a local copy for the close button [X] functionality
        var visible = IsVisible;
        if (ImGui.Begin("Graphics Settings", ref visible, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
        {
            Gui.Panel("Pipeline Features", () =>
            {
                ImGui.Checkbox("Enable Normal Mapping", ref UseNormalMap);
                ImGui.SliderFloat("Normal Strength", ref NormalStrength, 0.0f, 10.0f);
                ImGui.Checkbox("Enable Ambient Occlusion", ref UseAoMap);
                ImGui.SliderFloat("Ambient Occlusion Strength", ref AoMapStrength, 0.0f, 10.0f);
                ImGui.Checkbox("Enable Specular Mapping", ref UseSpecularMap);
                ImGui.SliderFloat("Specular Strength", ref SpecularMapStrength, 0.0f, 10.0f);
            });

            Gui.Panel("Atmospherics", () =>
            {
                ImGui.SliderFloat("Fog Near Offset", ref FogNearFactor, 0.0f, 1.0f);
                ImGui.SliderFloat("Fog Far Offset", ref FogFarFactor, 0.1f, 2.0f);
            });

            ImGui.Spacing();
            ImGui.Separator();

            if (ImGui.Button("Close", new Vector2(ImGui.GetContentRegionAvail().X, 0)))
            {
                visible = false;
            }

            ImGui.End();
        }

        if (IsVisible != visible)
        {
            IsVisible = visible;
            OnVisibilityChanged?.Invoke();
        }
    }
}