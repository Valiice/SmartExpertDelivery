using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using SmartExpertDelivery.Core;
using SmartExpertDelivery.Delivery;

namespace SmartExpertDelivery.Windows;

internal sealed class DeliveryOverlay : Window
{
    private readonly DeliveryRunner runner;
    private readonly Action openSettings;
    private float height;

    public DeliveryOverlay(DeliveryRunner runner, Action openSettings)
        : base(
            "SmartExpertDelivery###SmartExpertDeliveryOverlay",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing)
    {
        this.runner = runner;
        this.openSettings = openSettings;
        this.IsOpen = true;
        this.RespectCloseHotkey = false;
        this.PositionCondition = ImGuiCond.Always;
    }

    public override bool DrawConditions() => this.runner.ShowOverlay;

    public override void PreDraw()
    {
        if (SupplyList.IsOpen)
            this.Position = SupplyList.Position - new Vector2(0, this.height);
    }

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
            if (ImGui.Button($"Deliver {deliverable.Count} items"))
                this.runner.Start();
            ImGui.EndDisabled();
        }

        ListTooltip(deliverable, i => $"{i.Name}: {i.Entry.Seals} seals");

        ImGui.SameLine();
        ImGui.TextUnformatted($"{this.runner.Kept.Count} kept");
        ListTooltip(this.runner.Kept, i => $"{i.Name}: {Describe(i.Reason)}");

        ImGui.SameLine();
        if (ImGui.Button("Settings"))
            this.openSettings();

        if (this.runner.Status.Length > 0)
            ImGui.TextUnformatted(this.runner.Status);

        this.height = ImGui.GetWindowSize().Y;
    }

    private static void ListTooltip(IReadOnlyList<DeliveryItem> items, Func<DeliveryItem, string> line)
    {
        if (items.Count > 0 && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(string.Join('\n', items.Select(line)));
    }

    internal static string Describe(KeepReason reason) => reason switch
    {
        KeepReason.Gearset => "in a gear set",
        KeepReason.Materia => "has materia",
        KeepReason.Tome => "tomestone gear",
        KeepReason.LowerJob => "a lower job can use it",
        KeepReason.LockedJob => "a locked job can use it",
        _ => string.Empty,
    };
}
