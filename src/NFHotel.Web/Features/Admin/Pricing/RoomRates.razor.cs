using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Pricing;

/// <summary>
/// The code behind the room rates page: forwarding to <see cref="RoomRatesViewModel"/> with the token of the page.
/// </summary>
public partial class RoomRates : CancellableComponentBase
{
    /// <summary>Loads the rates when the page opens.</summary>
    protected override Task OnInitializedAsync() => ViewModel.LoadAsync(ComponentToken);

    private Task SaveAsync(RoomSize size) => ViewModel.SaveAsync(size, ComponentToken);
}
