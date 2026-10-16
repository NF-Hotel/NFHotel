using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.TestSupport;

/// <summary>
/// Builds bookings in a desired state from fixed dates.
/// </summary>
/// <remarks>
/// All dates are hard-coded — Domain takes "today" as a parameter (A-05), so no test
/// needs <c>DateTime.Now</c>. States are reached only through the real transitions, so
/// the factory can never produce a booking the domain could not itself have produced.
/// </remarks>
internal static class BookingFactory
{
    /// <summary>Today's date in all booking tests.</summary>
    internal static readonly DateOnly Today = new(2026, 1, 1);

    /// <summary>Default arrival. The same day as <see cref="Today"/>, so check-in is legal (BR-06).</summary>
    internal static readonly DateOnly Arrival = new(2026, 1, 1);

    /// <summary>Default departure (exclusive). Gives 9 nights.</summary>
    internal static readonly DateOnly Departure = new(2026, 1, 10);

    /// <summary>Default time of check-in.</summary>
    internal static readonly DateTimeOffset CheckInMoment = new(2026, 1, 1, 15, 0, 0, TimeSpan.Zero);

    /// <summary>Default time of check-out on the booked departure day.</summary>
    internal static readonly DateTimeOffset CheckOutMoment = new(2026, 1, 10, 10, 0, 0, TimeSpan.Zero);

    /// <summary>The room all default bookings are on.</summary>
    internal const int RoomId = 7;

    /// <summary>The guest all default bookings belong to.</summary>
    internal const int GuestId = 3;

    /// <summary>Standardperioden 1.–10. januar.</summary>
    internal static DateRange StandardPeriod => new(Arrival, Departure);

    /// <summary>
    /// Creates a booking in the start status <see cref="BookingStatus.Pending"/> (BR-01).
    /// </summary>
    /// <returns>A new booking on <see cref="RoomId"/> in <see cref="StandardPeriod"/>.</returns>
    internal static Booking Pending() => Booking.Create(StandardPeriod, RoomId, GuestId, Today);

    /// <summary>
    /// Creates a booking and runs it to the desired status by a legal route.
    /// </summary>
    /// <param name="status">The desired end state.</param>
    /// <returns>En booking i <paramref name="status"/>.</returns>
    internal static Booking InStatus(BookingStatus status)
    {
        var booking = Pending();

        switch (status)
        {
            case BookingStatus.Pending:
                break;

            case BookingStatus.Confirmed:
                booking.Confirm();
                break;

            case BookingStatus.CheckedIn:
                booking.CheckIn(CheckInMoment, Today);
                break;

            case BookingStatus.CheckedOut:
                booking.CheckIn(CheckInMoment, Today);

                // The check-out date must not lie after "today", so the hotel's date at
                // the check-out is the departure day — not the arrival day.
                booking.CheckOut(CheckOutMoment, Departure, Departure);
                break;

            case BookingStatus.Cancelled:
                booking.Cancel();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Ukendt bookingstatus.");
        }

        return booking;
    }

    /// <summary>
    /// Creates a booking where the guest left before the booked departure day (A-01).
    /// </summary>
    /// <param name="checkOutDate">
    /// The hotel-local date the guest actually left. Also used as "today", because a
    /// check-out is always registered on the day it happens.
    /// </param>
    /// <returns>A checked-out booking with <see cref="Booking.CheckOutDate"/> set.</returns>
    internal static Booking CheckedOutOn(DateOnly checkOutDate)
    {
        var booking = Pending();
        booking.CheckIn(CheckInMoment, Today);

        // The check-out time must be strictly after the check-in (BR-107); 10 o'clock
        // on the departure day lies after the check-in at 3 p.m. on the arrival day in all scenarios,
        // also on check-out the same day as arrival.
        var occurredAt = checkOutDate == Arrival
            ? CheckInMoment.AddHours(3)
            : new DateTimeOffset(checkOutDate.Year, checkOutDate.Month, checkOutDate.Day, 10, 0, 0, TimeSpan.Zero);

        booking.CheckOut(occurredAt, checkOutDate, checkOutDate);

        return booking;
    }

    /// <summary>
    /// Sets the primary key on a booking that is not persisted.
    /// </summary>
    /// <param name="booking">The booking that should have an id.</param>
    /// <param name="bookingId">The id EF Core would otherwise have assigned.</param>
    /// <returns>The same booking, with <see cref="Entity{TKey}.Id"/> set.</returns>
    /// <remarks>
    /// <see cref="Entity{TKey}.Id"/> has a private setter, because only EF Core may assign it
    /// (A-15). The booking number format of BR-109 can be tested only with a realistic id, so
    /// the key is set here via reflection instead of opening the setter to production code.
    /// </remarks>
    internal static Booking WithBookingId(Booking booking, int bookingId)
    {
        var setter = typeof(Booking).GetProperty(nameof(Booking.Id))!.GetSetMethod(nonPublic: true)!;
        setter.Invoke(booking, [bookingId]);

        return booking;
    }
}