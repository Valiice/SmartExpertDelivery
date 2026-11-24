using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using SmartExpertDelivery.Core;

namespace SmartExpertDelivery.Delivery;

internal static unsafe class PlayerGear
{
    private const uint PrioritySealAllowanceStatus = 1078;
    private const uint FreeCompanySealBuffStatus = 414;
    private const float PrioritySealAllowanceMultiplier = 1.15f;

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

    public static List<uint> CarriedItemIds() => ItemIds(Containers, meldedOnly: false);

    public static List<uint> OwnedItemIds() => ItemIds([.. Containers, InventoryType.EquippedItems], meldedOnly: false);

    public static HashSet<uint> MeldedItemIds() => [.. ItemIds(Containers, meldedOnly: true)];

    private static List<uint> ItemIds(InventoryType[] types, bool meldedOnly)
    {
        var ids = new List<uint>();
        var inventory = InventoryManager.Instance();
        foreach (var type in types)
        {
            var container = inventory->GetInventoryContainer(type);
            if (container == null)
                continue;
            for (var i = 0; i < container->GetSize(); i++)
            {
                var slot = container->GetInventorySlot(i);
                if (slot->ItemId != 0 && (!meldedOnly || slot->GetMateriaCount() > 0))
                    ids.Add(slot->ItemId);
            }
        }

        return ids;
    }

    public static HashSet<uint> GearsetItemIds()
    {
        var ids = new HashSet<uint>();
        var module = RaptureGearsetModule.Instance();
        for (var i = 0; i < module->Entries.Length; i++)
        {
            if (!module->IsValidGearset(i))
                continue;
            foreach (ref var item in module->Entries[i].Items)
            {
                if (item.ItemId != 0)
                    ids.Add(ItemUtil.GetBaseId(item.ItemId).ItemId);
            }
        }

        return ids;
    }
}
