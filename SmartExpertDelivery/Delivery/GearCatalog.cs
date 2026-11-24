using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace SmartExpertDelivery.Delivery;

internal sealed record GearEntry(string Name, int EquipLevel, uint ItemLevel, uint Slot, int[] JobIndexes, bool IsTome, uint Seals)
{
    private const uint RingSlot = 12;

    public int Wearable => this.Slot == RingSlot ? 2 : 1;

    public bool OutclassedBy(GearEntry other) =>
        other.Slot == this.Slot
        && other.EquipLevel == this.EquipLevel
        && other.ItemLevel > this.ItemLevel
        && !this.JobIndexes.Except(other.JobIndexes).Any();
}

internal sealed class GearCatalog
{
    private const byte TomestoneCurrencyType = 16;
    private static readonly byte[] DeliverableRarities = [2, 3, 7];

    private readonly Dictionary<uint, GearEntry?> cache = [];
    private readonly ExcelSheet<Item> items = Services.DataManager.GetExcelSheet<Item>();
    private readonly ExcelSheet<GCSupplyDutyReward> rewards = Services.DataManager.GetExcelSheet<GCSupplyDutyReward>();
    private readonly ExcelSheet<RawRow> categories = Services.DataManager.GetExcelSheet<RawRow>(name: "ClassJobCategory");
    private readonly Lazy<HashSet<uint>> tomeGear = new(FindTomeGear);

    public ClassJob[] Jobs { get; } = [.. Services.DataManager.GetExcelSheet<ClassJob>().Where(j => j.ExpArrayIndex >= 0)];

    public GearEntry? Find(uint itemId)
    {
        if (!this.cache.TryGetValue(itemId, out var entry))
            this.cache[itemId] = entry = this.Load(itemId);
        return entry;
    }

    private GearEntry? Load(uint itemId)
    {
        if (!this.items.TryGetRow(itemId, out var item))
            return null;

        // ClassJobCategory has one bool column per ClassJob row, after the name column
        var category = this.categories.GetRow(item.ClassJobCategory.RowId);
        var wearers = Enumerable.Range(0, this.Jobs.Length)
            .Where(i => category.ReadBoolColumn((int)this.Jobs[i].RowId + 1))
            .ToArray();
        var deliverable = item.EquipSlotCategory.RowId != 0 && item.Desynth != 0 && DeliverableRarities.Contains(item.Rarity);
        var seals = deliverable && this.rewards.TryGetRow(item.LevelItem.RowId, out var reward) ? reward.SealsExpertDelivery : 0;
        return new GearEntry(item.Name.ExtractText(), item.LevelEquip, item.LevelItem.RowId, item.EquipSlotCategory.RowId, wearers, this.tomeGear.Value.Contains(itemId), seals);
    }

    private static HashSet<uint> FindTomeGear()
    {
        var shops = Services.DataManager.GetExcelSheet<SpecialShop>();
        var tome = shops.Where(s => s.UseCurrencyType == TomestoneCurrencyType)
            .SelectMany(s => s.Item)
            .SelectMany(e => e.ReceiveItems)
            .Select(r => r.Item.RowId)
            .ToHashSet();

        var augmentedFromTome = shops.Where(s => s.UseCurrencyType != TomestoneCurrencyType)
            .SelectMany(s => s.Item)
            .Where(e => e.ItemCosts.Any(c => c.ItemCost.RowId != 0 && tome.Contains(c.ItemCost.RowId)))
            .SelectMany(e => e.ReceiveItems)
            .Select(r => r.Item.RowId)
            .ToList();

        tome.UnionWith(augmentedFromTome);
        tome.Remove(0);
        return tome;
    }
}
