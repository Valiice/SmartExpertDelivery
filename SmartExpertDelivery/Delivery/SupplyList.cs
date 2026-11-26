using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

namespace SmartExpertDelivery.Delivery;

internal readonly record struct SupplyEntry(int Index, uint ItemId, uint Seals);

internal static unsafe class SupplyList
{
    private const string ListAddon = "GrandCompanySupplyList";
    private const int ExpertTab = 2;
    private const int LoadedState = 2;
    private const int LoadStateValue = 0;
    private const int ItemCountValue = 6;
    private const int EntriesOffset = 0x288;
    private const int HandInCallback = 1;
    private const int ConfirmCallback = 0;

    private static readonly uint[] HandInPromptRows = [102434, 4605];

    private static readonly Lazy<string[]> HandInPrompts = new(() =>
    {
        var sheet = Services.DataManager.GetExcelSheet<Addon>();
        return Array.ConvertAll(HandInPromptRows, row => WithoutWhitespace(sheet.GetRow(row).Text.ExtractText()));
    });

    public static bool IsOpen => Find(ListAddon) != null;

    public static Vector2 Position
    {
        get
        {
            var addon = Find(ListAddon);
            return addon == null ? Vector2.Zero : new Vector2(addon->X, addon->Y);
        }
    }

    public static float Width
    {
        get
        {
            var addon = Find(ListAddon);
            return addon == null || addon->RootNode == null ? 0 : addon->RootNode->Width * addon->Scale;
        }
    }

    public static bool TryRead(List<SupplyEntry> entries)
    {
        entries.Clear();
        var addon = Find(ListAddon);
        if (addon == null || !addon->IsVisible || addon->AtkValuesCount <= ItemCountValue || addon->AtkValues[LoadStateValue].UInt != LoadedState)
            return false;

        // The entry layout only holds on the expert tab; on the others a handin would give away supply mission items.
        var list = (AddonGrandCompanySupplyList*)addon;
        if (list->SelectedTab != ExpertTab || list->ExpertDeliveryList == null || !list->ExpertDeliveryList->OwnerNode->IsVisible())
            return false;

        var raw = *(RawEntry**)((nint)addon + EntriesOffset);
        if (raw == null)
            return false;

        var count = (int)addon->AtkValues[ItemCountValue].UInt;
        for (var i = 0; i < count; i++)
            entries.Add(new SupplyEntry(i, ItemUtil.GetBaseId(raw[i].ItemId).ItemId, raw[i].Seals));
        return true;
    }

    public static void HandIn(int index)
    {
        var addon = Find(ListAddon);
        if (addon == null)
            return;
        var values = stackalloc AtkValue[3];
        values[0].SetInt(HandInCallback);
        values[1].SetInt(index);
        values[2] = default;
        addon->FireCallback(3, values, true);
    }

    public static bool ConfirmReward()
    {
        var addon = (AddonGrandCompanySupplyReward*)Find("GrandCompanySupplyReward");
        if (addon == null || !addon->IsVisible || !addon->DeliverButton->IsEnabled)
            return false;
        Confirm((AtkUnitBase*)addon);
        return true;
    }

    public static bool ConfirmPrompt()
    {
        var addon = (AddonSelectYesno*)Find("SelectYesno");
        if (addon == null || !addon->IsVisible || !addon->YesButton->IsEnabled)
            return false;
        if (!HandInPrompts.Value.Contains(WithoutWhitespace(addon->PromptText->NodeText.ToString())))
            return false;
        Confirm((AtkUnitBase*)addon);
        return true;
    }

    private static void Confirm(AtkUnitBase* addon)
    {
        var value = default(AtkValue);
        value.SetInt(ConfirmCallback);
        addon->FireCallback(1, &value, true);
    }

    private static string WithoutWhitespace(string text) => string.Concat(text.Where(c => !char.IsWhiteSpace(c)));

    private static AtkUnitBase* Find(string name)
    {
        var addon = Services.GameGui.GetAddonByName(name);
        return addon.IsNull || !addon.IsReady ? null : (AtkUnitBase*)addon.Address;
    }

    [StructLayout(LayoutKind.Explicit, Size = 152)]
    private struct RawEntry
    {
        [FieldOffset(120)] public uint Seals;
        [FieldOffset(132)] public uint ItemId;
    }
}
