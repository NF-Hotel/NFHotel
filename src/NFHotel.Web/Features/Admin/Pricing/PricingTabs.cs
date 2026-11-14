using NFHotel.Web.Components.Shared;

namespace NFHotel.Web.Features.Admin.Pricing;

/// <summary>The three pricing pages as tabs: one menu item, same routes. Defined once for the three pages.</summary>
public static class PricingTabs
{
    /// <summary>The tabs, in the order they are shown.</summary>
    public static IReadOnlyList<AdminTab> All { get; } =
    [
        new("/admin/pricing/rates", "Room rates"),
        new("/admin/pricing/suggestions", "Price suggestions"),
        new("/admin/pricing/settings", "Settings"),
    ];
}
