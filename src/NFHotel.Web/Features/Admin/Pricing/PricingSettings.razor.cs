using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Pricing;

/// <summary>
/// The code behind the pricing settings page: forwarding to <see cref="PricingSettingsViewModel"/> with the token
/// of the page.
/// </summary>
public partial class PricingSettings : CancellableComponentBase
{
    #region Protected methods

    /// <summary>Loads the settings when the page opens.</summary>
    protected override Task OnInitializedAsync() => ViewModel.LoadAsync(ComponentToken);

    #endregion Protected methods

    #region Private methods

    private Task SaveAsync() => ViewModel.SaveAsync(ComponentToken);

    #endregion Private methods
}
