using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The code behind the receipt card: the booking it shows and the one line about the room.
/// Which fields the card may show, and why, is explained in <c>BookingReceiptCard.razor</c> (BR-C-05, BR-C-06).
/// </summary>
public partial class BookingReceiptCard : ComponentBase
{
    /// <summary>The booking the receipt is for. No receipt is shown if it is <c>null</c>.</summary>
    [Parameter]
    [EditorRequired]
    public BookingDetailsDto? Details { get; set; }

    /// <summary>The room as the customer reads it: number, type and floor.</summary>
    private string RoomText => Details is null
        ? string.Empty
        : $"Room {Details.Room.RoomNumber} · {DisplayText.Describe(Details.Room.Size)} · {BookingFlowViewModel.DescribeFloor(Details.Room.Floor)}";
}
