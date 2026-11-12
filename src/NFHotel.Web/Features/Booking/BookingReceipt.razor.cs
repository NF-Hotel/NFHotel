using Microsoft.AspNetCore.Components;

using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The code behind the receipt lookup: the booking number from the address, handed to
/// <see cref="BookingReceiptViewModel"/> every time it changes, and the forwarding of the form.
/// </summary>
public partial class BookingReceipt : CancellableComponentBase
{
    /// <summary>The booking number from the address, for example <c>FLZ-000123</c>; <c>null</c> on <c>/booking</c>.</summary>
    [Parameter]
    public string? BookingNumber { get; set; }

    /// <summary>
    /// Fills in the number from the address — also when only the number in the address changes.
    /// </summary>
    protected override void OnParametersSet() => ViewModel.Prefill(BookingNumber);

    private Task LookUpAsync() => ViewModel.LookUpAsync(ComponentToken);
}
