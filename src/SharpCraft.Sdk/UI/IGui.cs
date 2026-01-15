using System.Numerics;

namespace SharpCraft.Sdk.UI;

/// <summary>
/// Abstraction for the GUI system to avoid exposing ImGui directly.
/// </summary>
public interface IGui
{
    void Text(string text, Vector4? color = null);
    void Property(string label, string value, Vector4? color = null);
    void Checkbox(string label, ref bool value);
    void SliderFloat(string label, ref float value, float min, float max, string format = "%.3f");
    void SliderInt(string label, ref int value, int min, int max);
    bool Button(string label, Vector2? size = null);
    void SameLine();
    void Separator();
    void Spacing();
    void Indent(float width = 20.0f);
    void Unindent(float width = 20.0f);

    bool Begin(string name, ref bool open, GuiWindowSettings settings = GuiWindowSettings.None);
    void End();

    bool BeginTabBar(string strId);
    void EndTabBar();
    bool BeginTabItem(string label);
    void EndTabItem();
    bool CollapsingHeader(string label);

    void SetWindowFontScale(float scale);
    void SetNextWindowPos(Vector2 pos, GuiCond cond = GuiCond.None, Vector2 pivot = default);
    void SetNextWindowSize(Vector2 size, GuiCond cond = GuiCond.None);

    Vector2 GetMainViewportCenter();
    Vector2 GetMainViewportSize();

    void DrawLine(Vector2 start, Vector2 end, Vector4 color, float thickness);
    void DrawImage(IntPtr textureId, Vector2 size, Vector2? uv0 = null, Vector2? uv1 = null, Vector4? tintCol = null, Vector4? borderCol = null);

    void Panel(string title, Action content);
    
    bool IsItemHovered();
    void SetTooltip(string text);

    bool BeginChild(string strId, Vector2 size = default, GuiFrameOptions frameOptions = GuiFrameOptions.None, GuiWindowSettings windowSettings = GuiWindowSettings.None);
    void EndChild();
    void SetScrollHereY(float centerYRatio = 0.5f);
    void SetKeyboardFocusHere(int offset = 0);
    bool InputText(string label, ref string input, uint maxLength, GuiInputTextOptions flags = GuiInputTextOptions.None);
    bool IsKeyPressed(GuiKey key);
    void PushStyleColor(GuiCol idx, Vector4 col);
    void PopStyleColor(int count = 1);
    void TextWrapped(string text);
    Vector2 GetContentRegionAvail();
}

[Flags]
public enum GuiFrameOptions
{
    None = 0,
    Border = 1 << 0,
    AlwaysUseWindowPadding = 1 << 1,
    AlwaysAutoResize = 1 << 2,
    FrameStyle = 1 << 3,
}

[Flags]
public enum GuiInputTextOptions
{
    None = 0,
    CharsDecimal = 1 << 0,
    CharsHexadecimal = 1 << 1,
    CharsUppercase = 1 << 2,
    CharsNoBlank = 1 << 3,
    AutoSelectAll = 1 << 4,
    EnterReturnsTrue = 1 << 5,
    CallbackCompletion = 1 << 6,
    CallbackHistory = 1 << 7,
    CallbackAlways = 1 << 8,
    CallbackCharFilter = 1 << 9,
    AllowTabInput = 1 << 10,
    CtrlEnterForNewLine = 1 << 11,
    NoHorizontalScroll = 1 << 12,
    AlwaysOverwrite = 1 << 13,
    ReadOnly = 1 << 14,
    Password = 1 << 15,
    NoUndoRedo = 1 << 16,
    CharsScientific = 1 << 17,
    CallbackResize = 1 << 18,
    CallbackEdit = 1 << 19,
    EscapeClearsAll = 1 << 20,
}

public enum GuiKey
{
    None = 0,
    Tab = 512,
    LeftArrow = 513,
    RightArrow = 514,
    UpArrow = 515,
    DownArrow = 516,
    PageUp = 517,
    PageDown = 518,
    Home = 519,
    End = 520,
    Insert = 521,
    Delete = 522,
    Backspace = 523,
    Space = 524,
    Enter = 525,
    Escape = 526,
    // ... add more as needed
}

public enum GuiCol
{
    Text = 0,
    TextDisabled = 1,
    WindowBg = 2,
    // ... add more as needed
}

[Flags]
public enum GuiWindowSettings
{
    None = 0,
    NoTitleBar = 1 << 0,
    NoResize = 1 << 1,
    NoMove = 1 << 2,
    NoScrollbar = 1 << 3,
    NoCollapse = 1 << 5,
    AlwaysAutoResize = 1 << 6,
    NoSavedSettings = 1 << 8,
    NoInputs = 1 << 9,
    NoDecoration = NoTitleBar | NoResize | NoScrollbar | NoCollapse
}

public enum GuiCond
{
    None = 0,
    Always = 1 << 0,
    Once = 1 << 1,
    FirstUseEver = 1 << 2,
    Appearing = 1 << 3
}
