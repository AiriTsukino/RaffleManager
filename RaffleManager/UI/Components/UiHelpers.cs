using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace RaffleManager.UI.Components;

internal static class UiHelpers
{
    public static void Header(string title, string? subtitle = null)
    {
        ImGui.TextColored(RaffleTheme.Pink, title);
        if (!string.IsNullOrWhiteSpace(subtitle)) TextMutedWrapped(subtitle);
        ImGui.Separator();
    }

    public static bool BeginCard(string id, Vector2 size = default)
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, RaffleTheme.PanelBg);
        ImGui.PushStyleColor(ImGuiCol.Border, RaffleTheme.Border);
        return ImGui.BeginChild(id, size, true);
    }

    public static void EndCard()
    {
        ImGui.EndChild();
        ImGui.PopStyleColor(2);
    }

    public static void TextMuted(string text) => ImGui.TextColored(RaffleTheme.Muted, text);
    public static void TextMutedWrapped(string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, RaffleTheme.Muted);
        ImGui.TextWrapped(text);
        ImGui.PopStyleColor();
    }

    public static string Gil(int amount) => $"{amount:N0} gil";

    public static bool InputIntGil(string label, ref int value, int step = 1000)
    {
        var changed = ImGui.InputInt(label, ref value, step, Math.Max(step * 10, step));
        if (value < 0) value = 0;
        return changed;
    }

    public static void ClippedTextWithTooltip(string text)
    {
        text ??= string.Empty;
        ImGui.TextUnformatted(text);
        if (ImGui.IsItemHovered() && ImGui.CalcTextSize(text).X > ImGui.GetItemRectSize().X)
            WrappedTooltip(text);
    }

    public static bool DrawSupportButton(string id, float width = 116f)
    {
        var supportLabel = $"      Support##{id}";
        var supportWidth = MathF.Max(width, GetSupportButtonWidth());
        RaffleTheme.PushKofiButton();
        var supportClicked = ImGui.Button(supportLabel, new Vector2(supportWidth, 0));
        RaffleTheme.PopKofiButton();
        DrawKofiCupIcon(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
        if (ImGui.IsItemHovered()) WrappedTooltip("Support me on Ko-Fi");
        if (supportClicked) OpenSupportLink();
        return supportClicked;
    }

    public static float GetSupportButtonWidth() => MathF.Max(116f, ImGui.CalcTextSize("Support").X + 52f);

    public static void TooltipOnHover(string text)
    {
        if (ImGui.IsItemHovered() && !string.IsNullOrWhiteSpace(text))
            WrappedTooltip(text);
    }

    public static void WrappedTooltip(string text, float wrapWidth = 420f)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + wrapWidth);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }

    public static void OpenSupportLink()
    {
        try { Process.Start(new ProcessStartInfo { FileName = "https://ko-fi.com/airitsukino", UseShellExecute = true }); }
        catch (Exception ex) { DalamudServices.Log.Warning(ex, "RaffleManager failed to open Ko-Fi link."); }
    }

    private static void DrawKofiCupIcon(Vector2 min, Vector2 max)
    {
        var draw = ImGui.GetWindowDrawList();
        var centerY = (min.Y + max.Y) * 0.5f;
        var cupMin = new Vector2(min.X + 11f, centerY - 5f);
        var cupMax = new Vector2(min.X + 25f, centerY + 5f);
        var color = ImGui.GetColorU32(new Vector4(0.96f, 0.91f, 1.00f, 1f));
        var shadow = ImGui.GetColorU32(new Vector4(0.20f, 0.07f, 0.36f, 0.9f));
        var heart = ImGui.GetColorU32(new Vector4(0.78f, 0.28f, 1.00f, 1f));
        draw.AddRectFilled(cupMin + new Vector2(1f, 1f), cupMax + new Vector2(1f, 1f), shadow, 3f);
        draw.AddRectFilled(cupMin, cupMax, color, 3f);
        draw.AddRect(new Vector2(cupMax.X - 1f, centerY - 3.5f), new Vector2(cupMax.X + 5.5f, centerY + 3.5f), color, 4f, 0, 2f);
        draw.AddCircleFilled(new Vector2(cupMin.X + 4.7f, centerY - 0.8f), 1.8f, heart);
        draw.AddCircleFilled(new Vector2(cupMin.X + 7.9f, centerY - 0.8f), 1.8f, heart);
        draw.AddTriangleFilled(new Vector2(cupMin.X + 3.0f, centerY + 0.2f), new Vector2(cupMin.X + 9.6f, centerY + 0.2f), new Vector2(cupMin.X + 6.3f, centerY + 4.2f), heart);
    }
}
