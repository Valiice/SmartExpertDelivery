using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace SmartExpertDelivery.Windows;

internal static class Theme
{
    public static readonly Vector4 Background = new(0.08f, 0.09f, 0.12f, 0.95f);
    public static readonly Vector4 Track = new(1f, 1f, 1f, 0.08f);
    public static readonly Vector4 Bright = new(0.93f, 0.94f, 0.97f, 1f);
    public static readonly Vector4 Muted = new(0.56f, 0.59f, 0.66f, 1f);
    public static readonly Vector4 Amber = new(0.98f, 0.69f, 0.28f, 1f);
    public static readonly Vector4 Blue = new(0.48f, 0.68f, 0.98f, 1f);
    public static readonly Vector4 Green = new(0.42f, 0.83f, 0.56f, 1f);

    public static void PushWindow()
    {
        var scale = ImGuiHelpers.GlobalScale;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10 * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(14, 12) * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Background);
    }

    public static void PopWindow()
    {
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(3);
    }

    public static bool HeaderButton(string icon, string hint)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.Text, Muted);
        var clicked = ImGui.SmallButton(icon);
        ImGui.PopStyleColor(2);
        ImGui.PopFont();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(hint);
        return clicked;
    }

    public static void Pill(string text, Vector4 color)
    {
        var padding = new Vector2(8, 2) * ImGuiHelpers.GlobalScale;
        var size = ImGui.CalcTextSize(text) + (padding * 2);
        var min = ImGui.GetCursorScreenPos();
        var draw = ImGui.GetWindowDrawList();
        draw.AddRectFilled(min, min + size, ImGui.GetColorU32(color with { W = 0.18f }), size.Y / 2);
        draw.AddText(min + padding, ImGui.GetColorU32(color), text);
        ImGui.Dummy(size);
    }
}
