using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using SmartExpertDelivery.Delivery;

namespace SmartExpertDelivery.Windows;

internal sealed class MainWindow : Window
{
    private const int MaxListLines = 15;
    private const float ListWidth = 450;

    private readonly Configuration config;
    private readonly DeliveryRunner runner;

    public MainWindow(Configuration config, DeliveryRunner runner)
        : base("Smart Expert Delivery###SmartExpertDeliveryMain", ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.config = config;
        this.runner = runner;
    }

    public override void Draw()
    {
        var keep = this.config.Keep;
        ImGui.TextUnformatted("Open Expert Delivery at your Grand Company and press Deliver.");
        ImGui.TextUnformatted("Gear matching a rule below stays in your inventory.");
        ImGui.Separator();

        this.Toggle("Keep gear my lower level jobs can still use", keep.LowerJobs, v => keep.LowerJobs = v, "Kept while any unlocked job that can equip it is below its equip level.");
        ImGui.Indent();
        this.Toggle("Also keep gear at a job's current level", keep.SameLevel, v => keep.SameLevel = v, "Level 50 gear is kept while a job that can equip it is exactly level 50.");
        this.Toggle("Only the best piece per slot", keep.BestOnly, v => keep.BestOnly = v, "Gear is not kept for a job when you own a higher item level piece for the same slot and equip level that it can wear.");
        ImGui.Unindent();
        this.Toggle("Keep gear for jobs I have not unlocked", keep.LockedJobs, v => keep.LockedJobs = v, "Counted from the job's starting level. A locked Blue Mage keeps all caster gear.");
        this.Toggle("Keep tomestone gear", keep.TomeGear, v => keep.TomeGear = v, "Includes the augmented versions.");
        this.Toggle("Keep gear with materia", keep.Materia, v => keep.Materia = v, "If one copy of an item has materia, every copy of it is kept.");
        this.Toggle("Keep gear that is part of a gear set", keep.Gearsets, v => keep.Gearsets = v, null);

        ImGui.Separator();
        if (!this.runner.Live)
            ImGui.TextUnformatted("Estimated from your inventory and armoury chest. The Expert Delivery window has the exact list.");

        if (this.runner.Deliverable.Count + this.runner.Kept.Count == 0)
        {
            ImGui.TextUnformatted("No gear for expert delivery found.");
            return;
        }

        var lines = Math.Min(this.runner.Deliverable.Count + this.runner.Kept.Count + 2, MaxListLines);
        if (ImGui.BeginChild("items", new Vector2(ListWidth * ImGuiHelpers.GlobalScale, lines * ImGui.GetTextLineHeightWithSpacing())))
        {
            ItemList("Will be delivered", this.runner.Deliverable, i => $"{i.Name}: {i.Entry.Seals} seals");
            ItemList("Kept", this.runner.Kept, i => $"{i.Name}: {DeliveryOverlay.Describe(i.Reason)}");
        }

        ImGui.EndChild();
    }

    private static void ItemList(string title, IReadOnlyList<DeliveryItem> items, Func<DeliveryItem, string> line)
    {
        ImGui.TextUnformatted($"{title} ({items.Count})");
        foreach (var item in items)
            ImGui.BulletText(line(item));
    }

    private void Toggle(string label, bool value, Action<bool> set, string? tooltip)
    {
        if (ImGui.Checkbox(label, ref value))
        {
            set(value);
            this.config.Save();
        }

        if (tooltip != null && ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);
    }
}
