using NFHotel.Application.Dto.Bookings;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Bookings;

/// <summary>
/// Eksplicit mapping fra bookingens læsemodel til dens DTO'er (A-14).
/// </summary>
/// <remarks>
/// "I dag" sendes ind som parameter og hentes ikke fra et ur her, så mapperne er rene
/// funktioner og kan enhedstestes uden fake-tid (A-05). Servicen ejer uret.
/// <para>
/// <see cref="ToActions(BookingStatus, DateOnly, DateTimeOffset?, DateTimeOffset?, DateOnly)"/>
/// er ikke mapping men regelevaluering, og den skal derfor være læsbar og testbar frem for
/// genereret.
/// </para>
/// </remarks>
public static class BookingMapping
{
    /// <summary>Præfiks på det viste bookingnummer (BR-109).</summary>
    private const string BookingNumberPrefix = "FLZ-";

    /// <summary>Cifferformat på det viste bookingnummer (BR-109).</summary>
    private const string BookingNumberFormat = "D6";

    /// <summary>
    /// Formaterer det viste bookingnummer, fx <c>FLZ-000123</c> (BR-109).
    /// </summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <returns>Bookingnummeret.</returns>
    /// <remarks>
    /// Spejler <see cref="Booking.BookingNumber"/>, som kun kan beregnes på entiteten.
    /// Læsestier har ingen entitet (A-11), så formatet findes nødvendigvis to steder —
    /// en paritetstest i Application.Tests skal pinne dem sammen.
    /// </remarks>
    public static string FormatBookingNumber(int bookingId) =>
        $"{BookingNumberPrefix}{bookingId.ToString(BookingNumberFormat, System.Globalization.CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Beregner hvilke handlinger der er lovlige på en booking lige nu (BR-22).
    /// </summary>
    /// <param name="status">Bookingens status.</param>
    /// <param name="startDate">Ankomstdato.</param>
    /// <param name="checkInTime">Faktisk indtjekningstidspunkt, eller <c>null</c>.</param>
    /// <param name="checkOutTime">Faktisk udtjekningstidspunkt, eller <c>null</c>.</param>
    /// <param name="today">Dagens dato i hotellets tidszone, fra <c>IClock.Today</c>.</param>
    /// <returns>Handlingerne.</returns>
    /// <remarks>
    /// Prædikaterne spejler <see cref="Booking.CanConfirm"/>, <see cref="Booking.CanCheckIn"/>,
    /// <see cref="Booking.CanCheckOut"/>, <see cref="Booking.CanCancel"/> og
    /// <see cref="Booking.CanReschedule"/>. De kan ikke kaldes direkte, fordi læsestien
    /// arbejder på en projektion og ikke på entiteten (A-11).
    /// <para>
    /// <b>Dette er den eneste kopi i Application</b>, og
    /// <see cref="ToActions(Booking, DateOnly)"/> kalder den samme funktion, så entitets- og
    /// læsestien ikke kan blive uenige. En paritetstest mod domænet skal pinne den — samme
    /// princip som constraint-paritetstesten i A-04.
    /// </para>
    /// </remarks>
    public static BookingActionsDto ToActions(
        BookingStatus status,
        DateOnly startDate,
        DateTimeOffset? checkInTime,
        DateTimeOffset? checkOutTime,
        DateOnly today)
    {
        var isOpen = status is BookingStatus.Pending or BookingStatus.Confirmed;

        return new BookingActionsDto(
            CanConfirm: status == BookingStatus.Pending,
            CanCheckIn: isOpen && checkInTime is null && startDate <= today,
            CanCheckOut: status == BookingStatus.CheckedIn && checkInTime is not null && checkOutTime is null,
            CanCancel: isOpen,
            CanReschedule: isOpen);
    }

    /// <summary>
    /// Beregner de lovlige handlinger for en booking-entitet (BR-22).
    /// </summary>
    /// <param name="booking">Bookingen.</param>
    /// <param name="today">Dagens dato i hotellets tidszone.</param>
    /// <returns>Handlingerne.</returns>
    public static BookingActionsDto ToActions(this Booking booking, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(booking);

        return ToActions(booking.Status, booking.StartDate, booking.CheckInTime, booking.CheckOutTime, today);
    }

    /// <summary>
    /// Mapper en læsemodel til oversigtens række.
    /// </summary>
    /// <param name="booking">Læsemodellen fra repositoryet.</param>
    /// <param name="today">Dagens dato i hotellets tidszone.</param>
    /// <returns>Rækken, med de lovlige handlinger beregnet.</returns>
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
            ToActions(booking.Status, booking.StartDate, booking.CheckInTime, booking.CheckOutTime, today));
    }

    /// <summary>
    /// Mapper en læsemodel til detaljevisningen.
    /// </summary>
    /// <param name="booking">Læsemodellen fra repositoryet.</param>
    /// <param name="today">Dagens dato i hotellets tidszone.</param>
    /// <returns>Detaljevisningen, med de lovlige handlinger beregnet.</returns>
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
            ToActions(booking.Status, booking.StartDate, booking.CheckInTime, booking.CheckOutTime, today));
    }

    /// <summary>
    /// Tæller bookede overnatninger via domænets <see cref="DateRange"/> (BR-109, BR-117).
    /// </summary>
    /// <param name="booking">Læsemodellen.</param>
    /// <returns>Antal nætter.</returns>
    /// <remarks>
    /// Beregningen hentes fra domænet frem for at skrives som en dagdifference her, så der
    /// kun findes én definition af "en nat".
    /// </remarks>
    private static int CountNights(BookingReadModel booking) =>
        new DateRange(booking.StartDate, booking.EndDate).Nights;
}