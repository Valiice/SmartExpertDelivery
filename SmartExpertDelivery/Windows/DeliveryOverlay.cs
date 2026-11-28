using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using SmartExpertDelivery.Delivery;

namespace SmartExpertDelivery.Windows;

internal sealed class DeliveryOverlay : Window
{
    private readonly Configuration config;
    private readonly DeliveryRunner runner;
    private Vector2 size;

    public DeliveryOverlay(Configuration config, DeliveryRunner runner)
        : base(
            "SmartExpertDelivery###SmartExpertDeliveryOverlay",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing)
    {
        this.config = config;
        this.runner = runner;
        this.IsOpen = true;
        this.RespectCloseHotkey = false;
        this.PositionCondition = ImGuiCond.Always;
    }

    public override bool DrawConditions() => this.runner.ShowOverlay;

    public override void PreDraw()
    {
        if (SupplyList.IsOpen)
            this.Position = SupplyList.Position + new Vector2(this.config.OverlayOnRight ? SupplyList.Width - this.size.X : 0, -this.size.Y);
        Theme.PushWindow();
    }

    public override void PostDraw() => Theme.PopWindow();

    public override void Draw()
    {
        var deliverable = this.runner.Deliverable;
        if (this.runner.Running)
        {
            if (ImGui.Button("Stop"))
                this.runner.Stop("Stopped.");
        }
        else
        {
            ImGui.BeginDisabled(deliverable.Count == 0);
            if (ImGui.Button(deliverable.Count == 0 ? "Nothing to deliver" : $"Deliver {deliverable.Count} items ({this.runner.SealTotal:N0} seals)"))
                this.runner.Start();
            ImGui.EndDisabled();
        }

        ListTooltip(deliverable, ItemText.Delivered);

        ImGui.SameLine();
        ImGui.TextColored(Theme.Muted, $"{this.runner.Kept.Count} kept");
        ListTooltip(this.runner.Kept, ItemText.Kept);

        if (this.runner.Status.Length > 0)
        {
            ImGui.SameLine();
            ImGui.TextColored(Theme.Amber, this.runner.Status);
        }

        this.size = ImGui.GetWindowSize();
    }

    private static void ListTooltip(IReadOnlyList<DeliveryItem> items, Func<DeliveryItem, string> line)
    {
        if (items.Count > 0 && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(string.Join('\n', items.Select(line)));
    }
}
