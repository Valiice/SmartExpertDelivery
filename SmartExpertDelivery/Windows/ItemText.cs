using SmartExpertDelivery.Core;
using SmartExpertDelivery.Delivery;

namespace SmartExpertDelivery.Windows;

internal static class ItemText
{
    public static string Delivered(DeliveryItem item) => $"{item.Name}: {Seals(item)}";

    public static string Kept(DeliveryItem item) => $"{item.Name}: {Reason(item)}";

    public static string Seals(DeliveryItem item) => $"{item.Entry.Seals} seals";

    public static string Reason(DeliveryItem item) => Describe(item.Reason);

    private static string Describe(KeepReason reason) => reason switch
    {
        KeepReason.Gearset => "in a gear set",
        KeepReason.Materia => "has materia",
        KeepReason.Spiritbond => "fully spiritbonded",
        KeepReason.Tome => "tomestone gear",
        KeepReason.LowerJob => "a lower job can use it",
        KeepReason.LockedJob => "a locked job can use it",
        _ => string.Empty,
    };
}
