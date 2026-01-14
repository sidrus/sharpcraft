using System.Numerics;
using ImGuiNET;
using SharpCraft.Client.UI.Components;
using SharpCraft.Core;
using SharpCraft.Client.Controllers;

namespace SharpCraft.Client.UI.Settings;

public class GraphicsSettingsHud : Hud
{
    public override string Name => "GraphicsSettingsHud";

    public bool IsVisible { get; set; }

    public bool UseNormalMap = true;
    public float NormalStrength = 0.5f;
    public bool UseAoMap = true;
    public float AoMapStrength = 0.5f;
    public bool UseSpecularMap = true;
    public float SpecularMapStrength = 0.5f;
    public bool VSync = false;
    public float Gamma = 1.6f;
    public float Exposure = 1.0f;

    public float FogNearFactor = 0.3f;
    public float FogFarFactor = 0.95f;
    public int RenderDistance = 8;

    public event Action? OnVisibilityChanged;

    public override void Draw(double deltaTime, HudContext context)
    {
        if (!IsVisible) return;

        // Force the window to center only on first appearance
        var io = ImGui.GetIO();
        ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X * 0.5f, io.DisplaySize.Y * 0.5f), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
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
                ImGui.Checkbox("VSync", ref VSync);
                ImGui.SliderFloat("Gamma Correction", ref Gamma, 0.0f, 4.0f);
                ImGui.SliderFloat("Exposure", ref Exposure, 0.0f, 10.0f);
            });

            Gui.Panel("Atmospherics", () =>
            {
                ImGui.SliderInt("Render Distance", ref RenderDistance, 2, 32);
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