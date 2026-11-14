using NFHotel.Web.Common;
using NFHotel.Web.Components.Shared;

namespace NFHotel.Web.Features.Admin.Pricing;

/// <summary>
/// The code behind the pricing settings page: forwarding to <see cref="PricingSettingsViewModel"/> with the token
/// of the page.
/// </summary>
public partial class PricingSettings : CancellableComponentBase
{
    #region Fields

    /// <summary>The three pricing pages as tabs: one menu item, same routes.</summary>
    private static readonly AdminTab[] PricingTabs =
    [
        new("/admin/pricing/rates", "Room rates"),
        new("/admin/pricing/suggestions", "Price suggestions"),
        new("/admin/pricing/settings", "Settings"),
    ];

    #endregion Fields

    #region Protected methods

    /// <summary>Loads the settings when the page opens.</summary>
    protected override Task OnInitializedAsync() => ViewModel.LoadAsync(ComponentToken);

    #endregion Protected methods

    #region Private methods

    private Task SaveAsync() => ViewModel.SaveAsync(ComponentToken);

    #endregion Private methods
}
