using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Pricing;

/// <summary>
/// The code behind the price suggestions page: forwarding to <see cref="PriceSuggestionsViewModel"/> with the token
/// of the page.
/// </summary>
public partial class PriceSuggestions : CancellableComponentBase
{
    #region Protected methods

    /// <summary>Loads the suggestions when the page opens.</summary>
    protected override Task OnInitializedAsync() => ViewModel.LoadAsync(ComponentToken);

    #endregion Protected methods

    #region Private methods

    private Task ReloadAsync() => ViewModel.LoadAsync(ComponentToken);

    private Task ApproveAsync(string key) => ViewModel.ApproveAsync(key, ComponentToken);

    private Task RevertAsync(int id) => ViewModel.RevertAsync(id, ComponentToken);

    #endregion Private methods
}
