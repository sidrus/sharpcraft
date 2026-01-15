using System.Numerics;
using ImGuiNET;
using SharpCraft.Sdk.UI;

namespace SharpCraft.Engine.UI;

/// <summary>
/// Implementation of <see cref="IGui"/> using ImGui.
/// </summary>
public class ImGuiGui : IGui
{
    public void Text(string text, Vector4? color = null)
    {
        if (color.HasValue)
            ImGui.TextColored(color.Value, text);
        else
            ImGui.Text(text);
    }

    public void Property(string label, string value, Vector4? color = null)
    {
        ImGui.Text($"{label}: ");
        ImGui.SameLine();
        if (color.HasValue)
            ImGui.TextColored(color.Value, value);
        else
            ImGui.Text(value);
    }

    public void Checkbox(string label, ref bool value)
    {
        ImGui.Checkbox(label, ref value);
    }

    public void SliderFloat(string label, ref float value, float min, float max, string format = "%.3f")
    {
        ImGui.SliderFloat(label, ref value, min, max, format);
    }

    public void SliderInt(string label, ref int value, int min, int max)
    {
        ImGui.SliderInt(label, ref value, min, max);
    }

    public bool Button(string label, Vector2? size = null)
    {
        return ImGui.Button(label, size ?? Vector2.Zero);
    }

    public void SameLine()
    {
        ImGui.SameLine();
    }

    public void Separator()
    {
        ImGui.Separator();
    }

    public void Spacing()
    {
        ImGui.Spacing();
    }

    public void Indent(float width = 20.0f)
    {
        ImGui.Indent(width);
    }

    public void Unindent(float width = 20.0f)
    {
        ImGui.Unindent(width);
    }

    public bool Begin(string name, ref bool open, GuiWindowSettings settings = GuiWindowSettings.None)
    {
        return ImGui.Begin(name, ref open, (ImGuiWindowFlags)settings);
    }

    public void End()
    {
        ImGui.End();
    }

    public bool BeginTabBar(string strId)
    {
        return ImGui.BeginTabBar(strId);
    }

    public void EndTabBar()
    {
        ImGui.EndTabBar();
    }

    public bool BeginTabItem(string label)
    {
        return ImGui.BeginTabItem(label);
    }

    public void EndTabItem()
    {
        ImGui.EndTabItem();
    }

    public bool CollapsingHeader(string label)
    {
        return ImGui.CollapsingHeader(label);
    }

    public void SetWindowFontScale(float scale)
    {
        ImGui.SetWindowFontScale(scale);
    }

    public void SetNextWindowPos(Vector2 pos, GuiCond cond = GuiCond.None, Vector2 pivot = default)
    {
        ImGui.SetNextWindowPos(pos, (ImGuiCond)cond, pivot);
    }

    public void SetNextWindowSize(Vector2 size, GuiCond cond = GuiCond.None)
    {
        ImGui.SetNextWindowSize(size, (ImGuiCond)cond);
    }

    public Vector2 GetMainViewportCenter()
    {
        return ImGui.GetMainViewport().GetCenter();
    }

    public Vector2 GetMainViewportSize()
    {
        return ImGui.GetMainViewport().Size;
    }

    public void DrawLine(Vector2 start, Vector2 end, Vector4 color, float thickness)
    {
        ImGui.GetForegroundDrawList().AddLine(start, end, ImGui.ColorConvertFloat4ToU32(color), thickness);
    }

    public void DrawImage(IntPtr textureId, Vector2 size, Vector2? uv0 = null, Vector2? uv1 = null, Vector4? tintCol = null, Vector4? borderCol = null)
    {
        ImGui.Image(textureId, size, uv0 ?? Vector2.Zero, uv1 ?? Vector2.One, tintCol ?? Vector4.One, borderCol ?? Vector4.Zero);
    }

    public void Panel(string title, Action content)
    {
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.0f, 1.0f, 1.0f, 1.0f), title.ToUpperInvariant());
        ImGui.Separator();
        ImGui.Indent(20.0f);

        content();

        ImGui.Unindent(20.0f);
    }

    public bool IsItemHovered()
    {
        return ImGui.IsItemHovered();
    }

    public void SetTooltip(string text)
    {
        ImGui.SetTooltip(text);
    }

    public bool BeginChild(string strId, Vector2 size = default, GuiFrameOptions frameOptions = GuiFrameOptions.None, GuiWindowSettings windowSettings = GuiWindowSettings.None)
    {
        return ImGui.BeginChild(strId, size, (ImGuiChildFlags)frameOptions, (ImGuiWindowFlags)windowSettings);
    }

    public void EndChild()
    {
        ImGui.EndChild();
    }

    public void SetScrollHereY(float centerYRatio = 0.5f)
    {
        ImGui.SetScrollHereY(centerYRatio);
    }

    public void SetKeyboardFocusHere(int offset = 0)
    {
        ImGui.SetKeyboardFocusHere(offset);
    }

    public bool InputText(string label, ref string input, uint maxLength, GuiInputTextOptions flags = GuiInputTextOptions.None)
    {
        return ImGui.InputText(label, ref input, maxLength, (ImGuiInputTextFlags)flags);
    }

    public bool IsKeyPressed(GuiKey key)
    {
        return ImGui.IsKeyPressed((ImGuiKey)key);
    }

    public void PushStyleColor(GuiCol idx, Vector4 col)
    {
        ImGui.PushStyleColor((ImGuiCol)idx, col);
    }

    public void PopStyleColor(int count = 1)
    {
        ImGui.PopStyleColor(count);
    }

    public void TextWrapped(string text)
    {
        ImGui.TextWrapped(text);
    }

    public Vector2 GetContentRegionAvail()
    {
        return ImGui.GetContentRegionAvail();
    }
}
