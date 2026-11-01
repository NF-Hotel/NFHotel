namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// Move a booking to new dates and/or a new room (BR-14, BR-17 to BR-20, BR-108).
/// </summary>
/// <remarks>
/// Deliberately not called <c>UpdateBookingRequest</c>: the use case is "move the booking", not
/// "set arbitrary fields". Status, check-in and check-out have their own use cases.
/// </remarks>
/// <param name="BookingId">The booking id.</param>
/// <param name="NewStartDate">New arrival date. Must be today or later (BR-108).</param>
/// <param name="NewEndDate">New departure date, exclusive.</param>
/// <param name="RoomId">The id of the room — the same as before, or a new room.</param>
public sealed record RescheduleBookingRequest(
    int BookingId,
    DateOnly NewStartDate,
    DateOnly NewEndDate,
    int RoomId);
