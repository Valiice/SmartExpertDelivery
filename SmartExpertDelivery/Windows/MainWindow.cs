using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using SmartExpertDelivery.Delivery;

namespace SmartExpertDelivery.Windows;

internal sealed class MainWindow : Window
{
    private const float MinWidth = 340;
    private const float IconSize = 32;
    private const float ListWidth = 360;
    private const float MaxListHeight = 320;
    private const float DockGap = 8;

    private readonly Configuration config;
    private readonly DeliveryRunner runner;
    private bool showSettings = true;
    private float listHeight = float.MaxValue;
    private CarriedItem? hovered;
    private Vector2 position;
    private Vector2 size;

    public MainWindow(Configuration config, DeliveryRunner runner)
        : base("Smart Expert Delivery###SmartExpertDeliveryMain", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar)
    {
        this.config = config;
        this.runner = runner;
        this.SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(MinWidth, 0), MaximumSize = new Vector2(float.MaxValue) };
    }

    public override void PreDraw()
    {
        // Sticks to the delivery window while it is open: on its left, or on its right when that would run off screen.
        this.Position = null;
        if (SupplyList.IsOpen)
        {
            var gap = DockGap * ImGuiHelpers.GlobalScale;
            var list = SupplyList.Position;
            var left = list.X - gap - this.size.X;
            this.Position = new Vector2(left >= 0 ? left : list.X + SupplyList.Width + gap, list.Y);
            this.PositionCondition = ImGuiCond.Always;
        }

        Theme.PushWindow();
    }

    public override void PostDraw() => Theme.PopWindow();

    // Runs before each draw, so the game tooltip goes away the frame after the hover does.
    public override void Update()
    {
        ItemTooltip.Show(this.hovered);
        ItemTooltip.PlaceBeside(this.position, this.size);
        this.hovered = null;
    }

    public override void OnClose() => ItemTooltip.Show(null);

    public override void Draw()
    {
        this.position = ImGui.GetWindowPos();
        this.size = ImGui.GetWindowSize();
        this.DrawHeader();
        if (this.showSettings)
            this.DrawSettings();

        if (this.runner.ItemCount == 0)
        {
            ImGui.TextColored(Theme.Muted, "No gear for expert delivery found.");
            return;
        }

        var scale = ImGuiHelpers.GlobalScale;
        if (ImGui.BeginChild("items", new Vector2(ListWidth * scale, Math.Min(this.listHeight, MaxListHeight * scale))))
        {
            this.ItemList($"WILL BE DELIVERED ({this.runner.Deliverable.Count})  {this.SealSummary()}", this.runner.Deliverable, Theme.Green, ItemText.Seals);
            this.ItemList($"KEPT ({this.runner.Kept.Count})", this.runner.Kept, Theme.Blue, ItemText.Reason);
            this.listHeight = ImGui.GetCursorPosY();
        }

        ImGui.EndChild();
    }

    private void DrawHeader()
    {
        ImGui.TextColored(Theme.Muted, "SMART EXPERT DELIVERY");
        ImGui.SameLine();
        ImGui.TextColored(this.runner.Live ? Theme.Green : Theme.Amber, this.runner.Live ? "live" : "estimate");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(this.runner.Live ? "Read from the Expert Delivery window." : "Estimated from your inventory and armoury chest. The Expert Delivery window has the exact list.");

        // No title bar, so the header buttons are ours.
        var style = ImGui.GetStyle();
        var cog = FontAwesomeIcon.Cog.ToIconString();
        var times = FontAwesomeIcon.Times.ToIconString();
        ImGui.PushFont(UiBuilder.IconFont);
        var width = ImGui.CalcTextSize(cog).X + ImGui.CalcTextSize(times).X + (style.FramePadding.X * 4) + style.ItemSpacing.X;
        ImGui.PopFont();
        ImGui.SameLine(ImGui.GetWindowWidth() - style.WindowPadding.X - width);
        if (Theme.HeaderButton(cog, "Keep rules"))
            this.showSettings = !this.showSettings;
        ImGui.SameLine();
        if (Theme.HeaderButton(times, "Hide. /sed brings it back."))
            this.IsOpen = false;
    }

    private void DrawSettings()
    {
        var keep = this.config.Keep;
        ImGui.TextColored(Theme.Muted, "Open Expert Delivery at your Grand Company and press Deliver.");
        ImGui.TextColored(Theme.Muted, "Gear matching a rule below stays in your inventory.");

        ImGui.PushStyleColor(ImGuiCol.FrameBg, Theme.Track);
        ImGui.PushStyleColor(ImGuiCol.CheckMark, Theme.Amber);
        this.Toggle("Keep gear my lower level jobs can still use", keep.LowerJobs, v => keep.LowerJobs = v, "Kept while any unlocked job that can equip it is below its equip level.");
        ImGui.Indent();
        this.Toggle("Also keep gear at a job's current level", keep.SameLevel, v => keep.SameLevel = v, "Level 50 gear is kept while a job that can equip it is exactly level 50.");
        this.Toggle("Only the best piece per slot", keep.BestOnly, v => keep.BestOnly = v, "Gear is not kept for a job when you own a higher item level piece for the same slot and equip level that it can wear.");
        ImGui.Unindent();
        this.Toggle("Keep gear for jobs I have not unlocked", keep.LockedJobs, v => keep.LockedJobs = v, "Counted from the job's starting level. A locked Blue Mage keeps all caster gear.");
        this.Toggle("Keep tomestone gear", keep.TomeGear, v => keep.TomeGear = v, "Includes the augmented versions.");
        this.Toggle("Keep gear with materia", keep.Materia, v => keep.Materia = v, "Checked per item: an unmelded copy of the same item is still delivered.");
        this.Toggle("Keep fully spiritbonded gear", keep.Spiritbond, v => keep.Spiritbond = v, "Gear at 100% spiritbond, so you can extract its materia first.");
        this.Toggle("Keep gear that is part of a gear set", keep.Gearsets, v => keep.Gearsets = v, "High quality and normal quality count as different items. Spare identical copies are delivered.");
        this.Toggle("Show the Deliver button on the right", this.config.OverlayOnRight, v => this.config.OverlayOnRight = v, "Moves it to the right corner of the Expert Delivery window, out of the way of other plugins' buttons.");
        ImGui.PopStyleColor(2);
        ImGui.Dummy(new Vector2(0, 4 * ImGuiHelpers.GlobalScale));
    }

    private string SealSummary()
    {
        var total = this.runner.SealTotal;
        var room = PlayerGear.SealRoom;
        return total > room ? $"{total:N0} seals, only {room:N0} fit under your cap" : $"{total:N0} seals";
    }

    private void ItemList(string title, IReadOnlyList<DeliveryItem> items, Vector4 color, Func<DeliveryItem, string> pill)
    {
        if (items.Count == 0)
            return;

        var scale = ImGuiHelpers.GlobalScale;
        ImGui.TextColored(Theme.Muted, title);
        foreach (var item in items)
        {
            var icon = Services.TextureProvider.GetFromGameIcon(new GameIconLookup(item.Icon, itemHq: item.Entry.Item.Hq)).GetWrapOrEmpty();
            ImGui.BeginGroup();
            ImGui.Image(icon.Handle, new Vector2(IconSize * scale));
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.TextColored(Theme.Bright, item.Name);
            Theme.Pill(pill(item), color);
            ImGui.EndGroup();
            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
                this.hovered = item.Entry.Item;
            ImGui.Dummy(new Vector2(0, 2 * scale));
        }
    }

    private void Toggle(string label, bool value, Action<bool> set, string tooltip)
    {
        if (ImGui.Checkbox(label, ref value))
        {
            set(value);
            this.config.Save();
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);
    }
}
