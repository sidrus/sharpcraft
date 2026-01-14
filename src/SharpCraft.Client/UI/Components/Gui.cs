using System.Numerics;
using ImGuiNET;

namespace SharpCraft.Client.UI.Components;

public static class Gui
{

    public static void Label(string label, bool? visible = true, Vector4? color = null)
    {
        if (visible.HasValue && !visible.Value)
        {
            return;
        }

        if (color.HasValue)
        {
            ImGui.TextColored(color.Value, label);
        }
        else
        {
            ImGui.Text(label);
        }
    }

    public static void Property(string label, string value, bool? visible = true, Vector4? color = null)
    {
        if (visible.HasValue && !visible.Value)
        {
            return;
        }

        ImGui.Text($"{label}: ");
        ImGui.SameLine();
        if (color.HasValue)
        {
            ImGui.TextColored(color.Value, value);
        }
        else
        {
            ImGui.Text(value);
        }
    }

    public static void Panel(string title, Action content)
    {
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.0f, 1.0f, 1.0f, 1.0f), title.ToUpperInvariant());
        ImGui.Separator();
        ImGui.Indent(20.0f);

        content();

        ImGui.Unindent(20.0f);
    }
}