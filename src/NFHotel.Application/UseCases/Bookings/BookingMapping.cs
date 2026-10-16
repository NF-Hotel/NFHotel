using NFHotel.Application.Dto.Bookings;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Bookings;

/// <summary>
/// Explicit mapping from the read model of the booking to its DTOs (A-14).
/// </summary>
/// <remarks>
/// "Today" is passed in as a parameter and is not fetched from a clock here, so the mappers are pure
/// functions and can be unit tested without fake time (A-05). The service owns the clock.
/// <para>
/// <see cref="ToActions(BookingStatus, DateOnly, DateTimeOffset?, DateTimeOffset?, DateOnly, bool)"/>
/// is not mapping but rule evaluation, and it must therefore be readable and testable rather than
/// genereret.
/// </para>
/// </remarks>
public static class BookingMapping
{
    /// <summary>Prefix of the displayed booking number (BR-109).</summary>
    private const string BookingNumberPrefix = "FLZ-";

    /// <summary>Digit format of the displayed booking number (BR-109).</summary>
    private const string BookingNumberFormat = "D6";

    /// <summary>
    /// Formats the displayed booking number, for example <c>FLZ-000123</c> (BR-109).
    /// </summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <returns>Bookingnummeret.</returns>
    /// <remarks>
    /// Mirrors <see cref="Booking.BookingNumber"/>, which can be computed only on the entity.
    /// Read paths have no entity (A-11), so the format necessarily exists in two places —
    /// a parity test in Application.Tests must pin them together.
    /// </remarks>
    public static string FormatBookingNumber(int bookingId) =>
        $"{BookingNumberPrefix}{bookingId.ToString(BookingNumberFormat, System.Globalization.CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Computes which actions are legal on a booking right now (BR-22).
    /// </summary>
    /// <param name="status">Bookingens status.</param>
    /// <param name="startDate">Arrival date.</param>
    /// <param name="checkInTime">Actual check-in time, or <c>null</c>.</param>
    /// <param name="checkOutTime">Actual check-out time, or <c>null</c>.</param>
    /// <param name="today">Today's date in the hotel's time zone, from <c>IClock.Today</c>.</param>
    /// <param name="roomIsBookable">Whether the room of the booking can be let (BR-110); a guest is checked in only to such a room (MIL-013).</param>
    /// <returns>Handlingerne.</returns>
    /// <remarks>
    /// The predicates mirror <see cref="Booking.CanConfirm"/>, <see cref="Booking.CanCheckIn"/>,
    /// <see cref="Booking.CanCheckOut"/>, <see cref="Booking.CanCancel"/> and
    /// <see cref="Booking.CanReschedule"/>. They cannot be called directly, because the read path
    /// works on a projection and not on the entity (A-11).
    /// <para>
    /// <b>This is the only copy in Application</b>, and
    /// <see cref="ToActions(Booking, DateOnly)"/> calls the same function, so the entity path and the
    /// read path cannot disagree. A parity test against the domain must pin it — the same
    /// principle as the constraint parity test in A-04.
    /// </para>
    /// </remarks>
    public static BookingActionsDto ToActions(
        BookingStatus status,
        DateOnly startDate,
        DateTimeOffset? checkInTime,
        DateTimeOffset? checkOutTime,
        DateOnly today,
        bool roomIsBookable = true)
    {
        var isOpen = status is BookingStatus.Pending or BookingStatus.Confirmed;

        return new BookingActionsDto(
            CanConfirm: status == BookingStatus.Pending,
            CanCheckIn: isOpen && checkInTime is null && startDate <= today && roomIsBookable,
            CanCheckOut: status == BookingStatus.CheckedIn && checkInTime is not null && checkOutTime is null,
            CanCancel: isOpen,
            CanReschedule: isOpen);
    }

    /// <summary>
    /// Beregner de lovlige handlinger for en booking-entitet (BR-22).
    /// </summary>
    /// <param name="booking">The booking.</param>
    /// <param name="today">Today's date in the hotel's time zone.</param>
    /// <returns>Handlingerne.</returns>
    public static BookingActionsDto ToActions(this Booking booking, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(booking);

        return ToActions(booking.Status, booking.StartDate, booking.CheckInTime, booking.CheckOutTime, today);
    }

    /// <summary>
    /// Maps a read model to a row of the overview.
    /// </summary>
    /// <param name="booking">The read model from the repository.</param>
    /// <param name="today">Today's date in the hotel's time zone.</param>
    /// <returns>The row, with the legal actions computed.</returns>
    public static BookingListItemDto ToListItem(this BookingReadModel booking, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(booking);

        return new BookingListItemDto(
            booking.BookingId,
            FormatBookingNumber(booking.BookingId),
            booking.StartDate,
            booking.EndDate,
            booking.EffectiveEndDate,
            CountNights(booking),
            booking.Status,
            booking.CheckInTime,
            booking.CheckOutTime,
            booking.Room.RoomId,
            booking.Room.RoomNumber,
            booking.Guest.GuestId,
            booking.Guest.FullName,
            booking.Guest.Country,
            booking.Guest.Email,
            ToActions(booking.Status, booking.StartDate, booking.CheckInTime, booking.CheckOutTime, today, RoomIsBookable(booking)));
    }

    /// <summary>
    /// Maps a read model to the detail view.
    /// </summary>
    /// <param name="booking">The read model from the repository.</param>
    /// <param name="today">Today's date in the hotel's time zone.</param>
    /// <returns>The detail view, with the legal actions computed.</returns>
    public static BookingDetailsDto ToDetails(this BookingReadModel booking, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(booking);

        return new BookingDetailsDto(
            booking.BookingId,
            FormatBookingNumber(booking.BookingId),
            booking.StartDate,
            booking.EndDate,
            booking.EffectiveEndDate,
            CountNights(booking),
            booking.Status,
            booking.CheckInTime,
            booking.CheckOutTime,
            booking.CheckOutDate,
            booking.Room,
            booking.Guest,
            ToActions(booking.Status, booking.StartDate, booking.CheckInTime, booking.CheckOutTime, today, RoomIsBookable(booking)));
    }

    /// <summary>
    /// Counts booked nights via the domain's <see cref="DateRange"/> (BR-109, BR-117).
    /// </summary>
    /// <param name="booking">The read model.</param>
    /// <returns>The number of nights.</returns>
    /// <remarks>
    /// The calculation is taken from the domain rather than written as a day difference here, so there
    /// is only one definition of "a night".
    /// </remarks>
    private static int CountNights(BookingReadModel booking) =>
        new DateRange(booking.StartDate, booking.EndDate).Nights;

    private static bool RoomIsBookable(BookingReadModel booking) => booking.Room.Status == RoomStatus.Available;

}