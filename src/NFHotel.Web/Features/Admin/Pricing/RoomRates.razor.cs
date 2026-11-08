using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;
using NFHotel.Web.Components.Shared;

namespace NFHotel.Web.Features.Admin.Pricing;

/// <summary>
/// The code behind the room rates page: forwarding to <see cref="RoomRatesViewModel"/> with the token of the page.
/// </summary>
public partial class RoomRates : CancellableComponentBase
{
    /// <summary>The three pricing pages as tabs: one menu item, same routes.</summary>
    private static readonly AdminTab[] PricingTabs =
    [
        new("/admin/pricing/rates", "Room rates"),
        new("/admin/pricing/suggestions", "Price suggestions"),
        new("/admin/pricing/settings", "Settings"),
    ];

    /// <summary>Loads the rates when the page opens.</summary>
    protected override Task OnInitializedAsync() => ViewModel.LoadAsync(ComponentToken);

    private Task SaveAsync(RoomSize size) => ViewModel.SaveAsync(size, ComponentToken);
}
