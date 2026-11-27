using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using SmartExpertDelivery.Core;

namespace SmartExpertDelivery.Delivery;

internal readonly record struct CarriedItem(uint ItemId, bool Hq, bool Melded, bool Spiritbonded)
{
    public uint RawId => ItemUtil.GetRawId(this.ItemId, this.Hq ? ItemKind.Hq : ItemKind.Normal);
}

internal static unsafe class PlayerGear
{
    private const uint PrioritySealAllowanceStatus = 1078;
    private const uint FreeCompanySealBuffStatus = 414;
    private const float PrioritySealAllowanceMultiplier = 1.15f;
    private const ushort FullSpiritbond = 10000;

    private static readonly InventoryType[] Containers =
    [
        InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4,
        InventoryType.ArmoryMainHand, InventoryType.ArmoryOffHand, InventoryType.ArmoryHead, InventoryType.ArmoryBody,
        InventoryType.ArmoryHands, InventoryType.ArmoryLegs, InventoryType.ArmoryFeets, InventoryType.ArmoryEar,
        InventoryType.ArmoryNeck, InventoryType.ArmoryWrist, InventoryType.ArmoryRings,
    ];

    public static uint SealRoom
    {
        get
        {
            var inventory = InventoryManager.Instance();
            var company = PlayerState.Instance()->GrandCompany;
            return inventory->GetMaxCompanySeals(company) - inventory->GetCompanySeals(company);
        }
    }

    public static float SealMultiplier
    {
        get
        {
            var multiplier = 1f;
            var statuses = Services.ObjectTable.LocalPlayer?.StatusList;
            if (statuses == null)
                return multiplier;

            foreach (var status in statuses)
            {
                if (status.StatusId == PrioritySealAllowanceStatus)
                    return PrioritySealAllowanceMultiplier;
                if (status.StatusId == FreeCompanySealBuffStatus)
                    multiplier = 1f + (status.Param / 100f);
            }

            return multiplier;
        }
    }

    public static JobLevel[] JobLevels(ClassJob[] jobs) =>
        Array.ConvertAll(jobs, j => new JobLevel(Services.PlayerState.GetClassJobLevel(j), j.StartingLevel));

    public static List<CarriedItem> Carried() => Items(Containers);

    public static List<CarriedItem> Owned() => Items([.. Containers, InventoryType.EquippedItems]);

    public static CarriedItem? ItemAt(InventoryType type, int index, uint expectedItemId)
    {
        var container = InventoryManager.Instance()->GetInventoryContainer(type);
        if (container == null || index >= container->GetSize())
            return null;
        var slot = container->GetInventorySlot(index);
        return slot->ItemId == expectedItemId ? Describe(slot) : null;
    }

    private static List<CarriedItem> Items(InventoryType[] types)
    {
        var items = new List<CarriedItem>();
        var inventory = InventoryManager.Instance();
        foreach (var type in types)
        {
            var container = inventory->GetInventoryContainer(type);
            if (container == null)
                continue;
            for (var i = 0; i < container->GetSize(); i++)
            {
                var slot = container->GetInventorySlot(i);
                if (slot->ItemId != 0)
                    items.Add(Describe(slot));
            }
        }

        return items;
    }

    private static CarriedItem Describe(InventoryItem* slot) => new(slot->ItemId, slot->IsHighQuality(), slot->GetMateriaCount() > 0, slot->GetSpiritbondOrCollectability() >= FullSpiritbond);

    public static Dictionary<uint, int> MostCopiesWornByAnyGearset()
    {
        var needs = new Dictionary<uint, int>();
        var worn = new Dictionary<uint, int>();
        var module = RaptureGearsetModule.Instance();
        for (var i = 0; i < module->Entries.Length; i++)
        {
            if (!module->IsValidGearset(i))
                continue;
            worn.Clear();
            foreach (ref var item in module->Entries[i].Items)
            {
                if (item.ItemId != 0)
                    worn[item.ItemId] = worn.GetValueOrDefault(item.ItemId) + 1;
            }

            foreach (var (id, count) in worn)
                needs[id] = Math.Max(needs.GetValueOrDefault(id), count);
        }

        return needs;
    }
}
