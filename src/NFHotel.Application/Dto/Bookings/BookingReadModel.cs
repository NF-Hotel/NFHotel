using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// The repository's projection of a booking with its room and its guest (A-11).
/// </summary>
/// <remarks>
/// Read paths fetch this instead of the entity. It removes the trap where a mapper is called
/// on a booking fetched without navigation data and silently gives empty fields — and it is faster.
/// <para>
/// The type is not a DTO for the UI: it is raw read material that
/// <see cref="BookingMapping"/> turns into <see cref="BookingListItemDto"/> or
/// <see cref="BookingDetailsDto"/>. The reason for the extra step is
/// <see cref="BookingActionsDto"/>, which depends on "today" — and the date comes from the clock in
/// the service, not from the database (A-05).
/// </para>
/// </remarks>
/// <param name="BookingId">Bookingens id.</param>
/// <param name="StartDate">Arrival date.</param>
/// <param name="EndDate">Booked departure date.</param>
/// <param name="EffectiveEndDate">
/// Effective end date (A-01). Projected from the generated database column
/// <c>effective_end_date</c>, which computes the same expression as <c>Booking.EffectiveEndDate</c>.
/// </param>
/// <param name="Status">Bookingens livscyklustilstand.</param>
/// <param name="CheckInTime">Actual check-in time, or <c>null</c>.</param>
/// <param name="CheckOutTime">Actual check-out time, or <c>null</c>.</param>
/// <param name="CheckOutDate">Hotel-local check-out date, or <c>null</c>.</param>
/// <param name="Room">The room, projected along.</param>
/// <param name="Guest">The guest, projected along, with passport number.</param>
public sealed record BookingReadModel(
    int BookingId,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly EffectiveEndDate,
    BookingStatus Status,
    DateTimeOffset? CheckInTime,
    DateTimeOffset? CheckOutTime,
    DateOnly? CheckOutDate,
    RoomListItemDto Room,
    GuestListItemDto Guest);
