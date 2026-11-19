using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Web.Common;

/// <summary>
/// The days that the date fields of the pages offer, and the rule that a stay of the public site breaks, both taken
/// from the rules of the stay on <see cref="Booking"/> (BR-C-07, BR-N-08, MIL-086, ADR-0050), so a page never offers
/// or allows a stay that the API refuses.
/// </summary>
public static class StayDates
{
    #region Public methods

    /// <summary>The last arrival that a page offers: the day before the last departure.</summary>
    /// <param name="today">Today on the clock of the hotel.</param>
    /// <returns>The last arrival.</returns>
    public static DateOnly LatestArrival(DateOnly today) => Booking.LatestDeparture(today).AddDays(-1);

    /// <summary>
    /// The code of the first rule of the public site that a stay breaks: an arrival before today, a departure beyond the
    /// five years, more than 30 nights.
    /// </summary>
    /// <param name="checkIn">The arrival.</param>
    /// <param name="checkOut">The departure.</param>
    /// <param name="today">Today on the clock of the hotel.</param>
    /// <returns>
    /// The code, or <c>null</c> when the stay breaks none of them. Dates that do not make a stay yet (a missing arrival,
    /// a departure that is not after it) give <c>null</c> too: the fields and the service answer those with their own
    /// codes.
    /// </returns>
    public static string? PublicError(DateOnly checkIn, DateOnly checkOut, DateOnly today)
    {
        if (checkIn == default || checkOut <= checkIn)
        {
            return null;
        }

        DateRange stay = new(checkIn, checkOut);
        return Booking.CheckPeriod(stay, today) ?? Booking.CheckPublicNights(stay);
    }

    #endregion Public methods
}
