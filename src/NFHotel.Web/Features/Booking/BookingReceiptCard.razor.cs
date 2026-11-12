using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The code behind the receipt card: the receipt it shows and the one line about the room.
/// Which fields the card shows, and why, is explained in <c>BookingReceiptCard.razor</c> (BR-C-05, BR-C-06).
/// </summary>
public partial class BookingReceiptCard : ComponentBase
{
    /// <summary>The receipt of the booking. No receipt is shown if it is <c>null</c>.</summary>
    [Parameter]
    [EditorRequired]
    public BookingReceiptDto? Receipt { get; set; }

    /// <summary>
    /// The room as the customer reads it: the type only. The room itself is given at check-in, so reception can move
    /// the booking without the receipt telling the guest something that is no longer true.
    /// </summary>
    private string RoomText => Receipt is null
        ? string.Empty
        : $"{DisplayText.Describe(Receipt.RoomSize)} · the room number is given at check-in";
}
