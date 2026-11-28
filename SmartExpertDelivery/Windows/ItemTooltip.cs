using System.Numerics;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SmartExpertDelivery.Delivery;

namespace SmartExpertDelivery.Windows;

internal static unsafe class ItemTooltip
{
    // The game ties a tooltip to an addon; NamePlate is always loaded for when the delivery window is not.
    private static readonly string[] OwnerAddons = ["GrandCompanySupplyList", "NamePlate"];

    private const float Gap = 8;

    private static CarriedItem? shown;
    private static ushort ownerId;

    public static void Show(CarriedItem? item)
    {
        if (item == shown)
            return;

        var stage = AtkStage.Instance();
        if (shown != null)
            stage->TooltipManager.HideTooltip(ownerId);
        shown = null;

        if (item is not { } hovered || Owner() is var owner && owner == null)
            return;

        var args = default(AtkTooltipManager.AtkTooltipArgs);
        args.Ctor();
        args.ItemArgs.Kind = DetailKind.InventoryItem;
        args.ItemArgs.InventoryType = hovered.Container;
        args.ItemArgs.Slot = hovered.Slot;
        stage->TooltipManager.ShowTooltip(AtkTooltipType.Item, owner->Id, owner->RootNode, &args);
        ownerId = owner->Id;
        shown = item;
    }

    // Plugin windows draw over all game UI, so the tooltip has to sit next to the window, not under it.
    public static void PlaceBeside(Vector2 windowPosition, Vector2 windowSize)
    {
        var detail = Services.GameGui.GetAddonByName("ItemDetail");
        if (shown == null || detail.IsNull || !detail.IsVisible)
            return;

        var screen = ImGuiHelpers.MainViewport;
        var window = windowPosition - screen.Pos;
        var gap = Gap * ImGuiHelpers.GlobalScale;
        var right = window.X + windowSize.X + gap;
        var x = right + detail.ScaledWidth <= screen.Size.X ? right : window.X - gap - detail.ScaledWidth;
        var y = Math.Clamp(window.Y, 0, Math.Max(0, screen.Size.Y - detail.ScaledHeight));
        ((AtkUnitBase*)detail.Address)->SetPosition((short)x, (short)y);
    }

    private static AtkUnitBase* Owner()
    {
        foreach (var name in OwnerAddons)
        {
            var addon = Services.GameGui.GetAddonByName(name);
            if (!addon.IsNull && addon.IsReady)
                return (AtkUnitBase*)addon.Address;
        }

        return null;
    }
}
