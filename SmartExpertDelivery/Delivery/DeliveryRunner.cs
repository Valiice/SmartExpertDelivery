using Dalamud.Game.ClientState.Conditions;
using SmartExpertDelivery.Core;

namespace SmartExpertDelivery.Delivery;

internal sealed record DeliveryItem(SupplyEntry Entry, string Name, uint Icon, KeepReason Reason);

internal sealed class DeliveryRunner(Configuration config, GearCatalog catalog)
{
    private const int TickMilliseconds = 200;
    private const int MaxAttemptsPerItem = 10;

    private readonly List<SupplyEntry> entries = [];
    private readonly List<DeliveryItem> deliverable = [];
    private readonly List<DeliveryItem> kept = [];

    private long nextTick;
    private uint lastItem;
    private int lastCount;
    private int attempts;

    public bool Running { get; private set; }

    public string Status { get; private set; } = string.Empty;

    public IReadOnlyList<DeliveryItem> Deliverable => this.deliverable;

    public IReadOnlyList<DeliveryItem> Kept => this.kept;

    public bool PreviewInventory { get; set; }

    public bool Live { get; private set; }

    public int ItemCount => this.deliverable.Count + this.kept.Count;

    // Includes the seal buffs active right now; the game may round a seal or two differently.
    public long SealTotal => this.deliverable.Sum(i => (long)(i.Entry.Seals * PlayerGear.SealMultiplier));

    public bool ShowOverlay => this.Running || (this.Live && (this.Status.Length > 0 || this.ItemCount > 0));

    public void Start()
    {
        this.Running = true;
        this.Status = string.Empty;
        this.attempts = 0;
        Trace.Event($"start: {this.deliverable.Count} to deliver for {this.SealTotal} seals, {this.kept.Count} kept, room for {PlayerGear.SealRoom}, multiplier {PlayerGear.SealMultiplier}");
        foreach (var item in this.kept)
            Trace.Event($"  kept [{item.Entry.Index}] {item.Name}: {item.Reason}");
    }

    public void Stop(string status)
    {
        if (this.Running)
            Trace.Event($"stop: \"{status}\", {this.deliverable.Count} still deliverable, room for {PlayerGear.SealRoom}");
        this.Running = false;
        this.Status = status;
    }

    public void Update()
    {
        if (Environment.TickCount64 < this.nextTick)
            return;
        this.nextTick = Environment.TickCount64 + TickMilliseconds;

        if (this.Running && (SupplyList.ConfirmReward() || SupplyList.ConfirmPrompt()))
            return;

        if (!SupplyList.IsOpen)
        {
            var listHiddenByRewardWindow = this.Running && Services.Condition[ConditionFlag.OccupiedInQuestEvent];
            if (listHiddenByRewardWindow)
                return;
            this.Stop(string.Empty);
            this.Preview();
            return;
        }

        if (!SupplyList.TryRead(this.entries))
        {
            if (!this.Running)
                this.Preview();
            return;
        }

        this.Live = true;
        this.Evaluate();
        if (this.Running)
            this.DeliverNext();
    }

    private void Preview()
    {
        this.Live = false;
        if (!this.PreviewInventory)
        {
            this.Clear();
            return;
        }

        this.entries.Clear();
        foreach (var item in PlayerGear.Carried())
        {
            if (catalog.Find(item.ItemId) is { Seals: > 0 } gear)
                this.entries.Add(new SupplyEntry(this.entries.Count, item, gear.Seals));
        }

        this.Evaluate();
    }

    private void Clear()
    {
        this.deliverable.Clear();
        this.kept.Clear();
    }

    private void Evaluate()
    {
        this.Clear();
        var levels = PlayerGear.JobLevels(catalog.Jobs);
        var owned = PlayerGear.Owned();
        var ownedGear = config.Keep.BestOnly ? owned.Select(i => catalog.Find(i.ItemId)).OfType<GearEntry>().ToList() : [];
        var gearsetEntries = config.Keep.Gearsets ? this.EntriesGearsetsNeed(owned) : [];

        foreach (var entry in this.entries)
        {
            var gear = catalog.Find(entry.Item.ItemId);
            if (gear == null)
                continue;
            var reason = KeepRules.Check(
                new Gear(
                    gear.EquipLevel,
                    Array.ConvertAll(gear.JobIndexes, i => levels[i]),
                    gear.IsTome,
                    gear.Meldable && entry.Item.Melded,
                    gearsetEntries.Contains(entry.Index),
                    ownedGear.Count(gear.OutclassedBy) >= gear.Wearable,
                    entry.Item.Spiritbonded),
                config.Keep);
            (reason == KeepReason.None ? this.deliverable : this.kept).Add(new DeliveryItem(entry, gear.Name, gear.Icon, reason));
        }
    }

    private HashSet<int> EntriesGearsetsNeed(List<CarriedItem> owned)
    {
        var meldedFirst = this.entries.OrderByDescending(e => e.Item.Melded).ToList();
        var needed = KeepRules.ListedCopiesGearsetsNeed(
            PlayerGear.MostCopiesWornByAnyGearset(),
            owned.ConvertAll(i => i.RawId),
            meldedFirst.ConvertAll(e => e.Item.RawId));
        return [.. needed.Select(i => meldedFirst[i].Index)];
    }

    private void DeliverNext()
    {
        var room = PlayerGear.SealRoom;
        var multiplier = PlayerGear.SealMultiplier;
        var next = this.deliverable.Where(i => i.Entry.Seals * multiplier < room).MinBy(i => i.Entry.Seals);
        if (next == null)
        {
            this.Stop(this.deliverable.Count == 0 ? "Done." : "Seal cap reached, spend some seals first.");
            return;
        }

        var sameAttempt = next.Entry.Item.ItemId == this.lastItem && this.entries.Count == this.lastCount;
        this.attempts = sameAttempt ? this.attempts + 1 : 0;
        this.lastItem = next.Entry.Item.ItemId;
        this.lastCount = this.entries.Count;
        if (this.attempts >= MaxAttemptsPerItem)
        {
            this.Stop($"Stopped: {next.Name} would not hand in.");
            return;
        }

        Trace.Event($"hand in [{next.Entry.Index}] {next.Name} ({next.Entry.Item.Container} slot {next.Entry.Item.Slot}) for {next.Entry.Seals}, attempt {this.attempts + 1}, {this.entries.Count} listed, room for {room}");
        SupplyList.HandIn(next.Entry.Index);
    }
}
