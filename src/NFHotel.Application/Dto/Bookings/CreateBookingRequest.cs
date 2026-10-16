using NFHotel.Application.Dto.Guests;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// Create a booking (BR-01, BR-41 to BR-49, BR-51, BR-101 to BR-106).
/// </summary>
/// <remarks>
/// Carries BR-47: <b>exactly one</b> of <see cref="GuestId"/> and <see cref="NewGuest"/> must
/// be filled in. That both or neither is set is a validation error from the service — not a
/// <c>NullReferenceException</c> down in a ViewModel.
/// </remarks>
/// <param name="CheckInDate">Arrival date, inclusive.</param>
/// <param name="CheckOutDate">Departure date, exclusive. Must be after the arrival date (BR-103).</param>
/// <param name="RoomId">The id of the room (BR-45, BR-105).</param>
/// <param name="GuestId">The id of an existing guest, or <c>null</c> if the guest is created now.</param>
/// <param name="NewGuest">The fields for a new guest, or <c>null</c> if an existing one is chosen.</param>
public sealed record CreateBookingRequest(
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int RoomId,
    int? GuestId,
    GuestFields? NewGuest);