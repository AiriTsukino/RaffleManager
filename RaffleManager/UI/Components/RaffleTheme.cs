using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace RaffleManager.UI.Components;

internal static class RaffleTheme
{
    public static readonly Vector4 VoidPurple = new(0.21f, 0.00f, 0.61f, 1f);
    public static readonly Vector4 Indigo = new(0.33f, 0.31f, 0.65f, 1f);
    public static readonly Vector4 Orchid = new(0.59f, 0.40f, 0.88f, 1f);
    public static readonly Vector4 Pink = new(0.81f, 0.58f, 0.98f, 1f);
    public static readonly Vector4 Teal = new(0.00f, 0.76f, 0.73f, 1f);
    public static readonly Vector4 Text = new(0.92f, 0.88f, 0.99f, 1f);
    public static readonly Vector4 Muted = new(0.64f, 0.58f, 0.82f, 1f);
    public static readonly Vector4 PanelBg = new(0.13f, 0.10f, 0.26f, 0.96f);
    public static readonly Vector4 InputBg = new(0.08f, 0.07f, 0.16f, 1f);
    public static readonly Vector4 Border = new(0.23f, 0.17f, 0.45f, 1f);
    public static readonly Vector4 Danger = new(0.70f, 0.24f, 0.63f, 1f);

    private static int pushedColors;
    private static int pushedVars;

    public static void Push()
    {
        pushedColors = 0;
        pushedVars = 0;
        PushColor(ImGuiCol.Text, Text);
        PushColor(ImGuiCol.TextDisabled, Muted);
        PushColor(ImGuiCol.WindowBg, new Vector4(0.055f, 0.039f, 0.12f, 0.98f));
        PushColor(ImGuiCol.ChildBg, new Vector4(0.10f, 0.08f, 0.18f, 0.86f));
        PushColor(ImGuiCol.PopupBg, new Vector4(0.07f, 0.05f, 0.13f, 0.99f));
        PushColor(ImGuiCol.Border, Border);
        PushColor(ImGuiCol.FrameBg, InputBg);
        PushColor(ImGuiCol.FrameBgHovered, new Vector4(0.16f, 0.12f, 0.29f, 1f));
        PushColor(ImGuiCol.FrameBgActive, new Vector4(0.25f, 0.16f, 0.43f, 1f));
        PushColor(ImGuiCol.TitleBg, new Vector4(0.10f, 0.06f, 0.19f, 1f));
        PushColor(ImGuiCol.TitleBgActive, new Vector4(0.21f, 0.10f, 0.37f, 1f));
        PushColor(ImGuiCol.CheckMark, Pink);
        PushColor(ImGuiCol.SliderGrab, Orchid);
        PushColor(ImGuiCol.SliderGrabActive, Pink);
        PushColor(ImGuiCol.Button, new Vector4(0.19f, 0.12f, 0.34f, 1f));
        PushColor(ImGuiCol.ButtonHovered, new Vector4(0.33f, 0.20f, 0.56f, 1f));
        PushColor(ImGuiCol.ButtonActive, VoidPurple);
        PushColor(ImGuiCol.Header, new Vector4(0.21f, 0.13f, 0.37f, 1f));
        PushColor(ImGuiCol.HeaderHovered, new Vector4(0.35f, 0.18f, 0.58f, 1f));
        PushColor(ImGuiCol.HeaderActive, VoidPurple);
        PushColor(ImGuiCol.Separator, Border);
        PushColor(ImGuiCol.Tab, new Vector4(0.11f, 0.08f, 0.18f, 1f));
        PushColor(ImGuiCol.TabHovered, new Vector4(0.37f, 0.18f, 0.60f, 1f));
        PushColor(ImGuiCol.TabActive, new Vector4(0.25f, 0.13f, 0.43f, 1f));
        PushColor(ImGuiCol.TableHeaderBg, new Vector4(0.18f, 0.11f, 0.31f, 1f));
        PushColor(ImGuiCol.TableBorderStrong, Border);
        PushColor(ImGuiCol.TableBorderLight, new Vector4(0.20f, 0.13f, 0.35f, 1f));

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 9f); pushedVars++;
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 8f); pushedVars++;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6f); pushedVars++;
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 6f); pushedVars++;
        ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 6f); pushedVars++;
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 7)); pushedVars++;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 5)); pushedVars++;
    }

    public static void Pop()
    {
        if (pushedVars > 0) ImGui.PopStyleVar(pushedVars);
        if (pushedColors > 0) ImGui.PopStyleColor(pushedColors);
        pushedVars = 0;
        pushedColors = 0;
    }

    public static void PushKofiButton()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.42f, 0.15f, 0.78f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.55f, 0.23f, 0.96f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.30f, 0.10f, 0.58f, 1f));
    }

    public static void PopKofiButton() => ImGui.PopStyleColor(3);

    private static void PushColor(ImGuiCol col, Vector4 color)
    {
        ImGui.PushStyleColor(col, color);
        pushedColors++;
    }
}
