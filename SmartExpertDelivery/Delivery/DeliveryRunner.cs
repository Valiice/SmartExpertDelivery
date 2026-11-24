using Dalamud.Game.ClientState.Conditions;
using SmartExpertDelivery.Core;

namespace SmartExpertDelivery.Delivery;

internal sealed record DeliveryItem(SupplyEntry Entry, string Name, KeepReason Reason);

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

    public bool ShowOverlay => this.Running || (this.Live && (this.Status.Length > 0 || this.deliverable.Count + this.kept.Count > 0));

    public void Start()
    {
        this.Running = true;
        this.Status = string.Empty;
        this.attempts = 0;
    }

    public void Stop(string status)
    {
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
            // The game closes the list while the reward window is up, so a run only ends once the NPC dialogue does.
            if (this.Running && Services.Condition[ConditionFlag.OccupiedInQuestEvent])
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
        foreach (var id in PlayerGear.CarriedItemIds())
        {
            if (catalog.Find(id) is { Seals: > 0 } gear)
                this.entries.Add(new SupplyEntry(this.entries.Count, id, gear.Seals));
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
        var melded = config.Keep.Materia ? PlayerGear.MeldedItemIds() : [];
        var gearsets = config.Keep.Gearsets ? PlayerGear.GearsetItemIds() : [];
        var owned = config.Keep.BestOnly ? PlayerGear.OwnedItemIds().Select(catalog.Find).OfType<GearEntry>().ToList() : [];

        foreach (var entry in this.entries)
        {
            var gear = catalog.Find(entry.ItemId);
            if (gear == null)
                continue;
            var reason = KeepRules.Check(
                new Gear(
                    gear.EquipLevel,
                    Array.ConvertAll(gear.JobIndexes, i => levels[i]),
                    gear.IsTome,
                    melded.Contains(entry.ItemId),
                    gearsets.Contains(entry.ItemId),
                    owned.Count(gear.OutclassedBy) >= gear.Wearable),
                config.Keep);
            (reason == KeepReason.None ? this.deliverable : this.kept).Add(new DeliveryItem(entry, gear.Name, reason));
        }
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

        var sameAttempt = next.Entry.ItemId == this.lastItem && this.entries.Count == this.lastCount;
        this.attempts = sameAttempt ? this.attempts + 1 : 0;
        this.lastItem = next.Entry.ItemId;
        this.lastCount = this.entries.Count;
        if (this.attempts >= MaxAttemptsPerItem)
        {
            this.Stop($"Stopped: {next.Name} would not hand in.");
            return;
        }

        SupplyList.HandIn(next.Entry.Index);
    }
}
