using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// A booking as it is shown in the overview.
/// </summary>
/// <param name="BookingId">The booking id.</param>
/// <param name="BookingNumber">Displayed booking number, for example <c>FLZ-000123</c> (BR-109).</param>
/// <param name="StartDate">Arrival date.</param>
/// <param name="EndDate">Booked departure date. History — never changed by a check-out (A-01).</param>
/// <param name="EffectiveEndDate">
/// The date up to which the booking actually occupies the room (A-01). Differs from
/// <paramref name="EndDate"/> when the guest has left early.
/// </param>
/// <param name="Nights">Number of booked nights (BR-109, BR-117).</param>
/// <param name="Status">The lifecycle state of the booking.</param>
/// <param name="CheckInTime">Actual check-in time, or <c>null</c>.</param>
/// <param name="CheckOutTime">Actual check-out time, or <c>null</c>.</param>
/// <param name="RoomId">The room id.</param>
/// <param name="RoomNumber">The room number of the room.</param>
/// <param name="GuestId">The id of the guest.</param>
/// <param name="GuestFullName">The full name of the guest (BR-25).</param>
/// <param name="GuestCountry">The country of the guest.</param>
/// <param name="GuestEmail">The email of the guest.</param>
/// <param name="Actions">Which actions are legal right now (BR-22).</param>
public sealed record BookingListItemDto(
    int BookingId,
    string BookingNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly EffectiveEndDate,
    int Nights,
    BookingStatus Status,
    DateTimeOffset? CheckInTime,
    DateTimeOffset? CheckOutTime,
    int RoomId,
    string RoomNumber,
    int GuestId,
    string GuestFullName,
    string GuestCountry,
    string GuestEmail,
    BookingActionsDto Actions);
