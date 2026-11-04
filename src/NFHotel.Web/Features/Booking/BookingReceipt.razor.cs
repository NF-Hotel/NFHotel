using Microsoft.AspNetCore.Components;

using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The code behind the bookmarkable receipt: the booking number from the address, handed to
/// <see cref="BookingReceiptViewModel"/> every time it changes.
/// </summary>
public partial class BookingReceipt : CancellableComponentBase
{
    /// <summary>The booking number from the address, for example <c>FLZ-000123</c>.</summary>
    [Parameter]
    public string? BookingNumber { get; set; }

    /// <summary>
    /// Looks up the booking — also when only the number in the address changes.
    /// </summary>
    protected override Task OnParametersSetAsync() => ViewModel.LoadAsync(BookingNumber, ComponentToken);
}
