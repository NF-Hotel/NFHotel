using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// A booking with everything the detail screen needs.
/// </summary>
/// <param name="BookingId">Bookingens id.</param>
/// <param name="BookingNumber">Vist bookingnummer (BR-109).</param>
/// <param name="StartDate">Arrival date.</param>
/// <param name="EndDate">Booked departure date (history, A-01).</param>
/// <param name="EffectiveEndDate">The date up to which the room is actually occupied (A-01).</param>
/// <param name="Nights">Antal bookede overnatninger.</param>
/// <param name="Status">Bookingens livscyklustilstand.</param>
/// <param name="CheckInTime">Actual check-in time, or <c>null</c>.</param>
/// <param name="CheckOutTime">Actual check-out time, or <c>null</c>.</param>
/// <param name="CheckOutDate">The hotel-local date the guest checked out, or <c>null</c> (A-02, A-03).</param>
/// <param name="Room">The room.</param>
/// <param name="Guest">The guest, without passport number.</param>
/// <param name="Actions">Which actions are legal right now (BR-22).</param>
public sealed record BookingDetailsDto(
    int BookingId,
    string BookingNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly EffectiveEndDate,
    int Nights,
    BookingStatus Status,
    DateTimeOffset? CheckInTime,
    DateTimeOffset? CheckOutTime,
    DateOnly? CheckOutDate,
    RoomListItemDto Room,
    GuestListItemDto Guest,
    BookingActionsDto Actions);