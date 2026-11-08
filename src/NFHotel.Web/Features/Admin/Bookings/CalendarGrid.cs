using System.Globalization;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Web.Features.Admin.Bookings;

/// <summary>
/// The month grid of the availability calendar: whole weeks from Monday to Sunday around the month, with the figures
/// of each night.
/// </summary>
/// <remarks>
/// Pure, so the arithmetic of weeks and months is tested without a page. The counting itself is the service's
/// (<see cref="Application.UseCases.Bookings.IBookingService.GetAvailabilityAsync"/>); this class only lays it out.
/// </remarks>
public static class CalendarGrid
{
    /// <summary>The first day of the month of a date.</summary>
    /// <param name="date">Any day of the month.</param>
    /// <returns>The 1st of that month.</returns>
    public static DateOnly MonthOf(DateOnly date) => new(date.Year, date.Month, 1);

    /// <summary>The Monday the grid of a month starts on: the 1st, or the Monday before it.</summary>
    /// <param name="month">The 1st of the month.</param>
    /// <returns>The first day shown.</returns>
    public static DateOnly FirstShown(DateOnly month)
    {
        int daysAfterMonday = ((int)month.DayOfWeek + 6) % 7;

        return month.AddDays(-daysAfterMonday);
    }

    /// <summary>The day after the Sunday the grid of a month ends on, so the grid is a half-open period like a stay.</summary>
    /// <param name="month">The 1st of the month.</param>
    /// <returns>The first day not shown.</returns>
    public static DateOnly EndShown(DateOnly month)
    {
        DateOnly last = month.AddMonths(1).AddDays(-1);
        int daysToSunday = (7 - (int)last.DayOfWeek) % 7;

        return last.AddDays(daysToSunday + 1);
    }

    /// <summary>Lays out the days of a month.</summary>
    /// <param name="month">The 1st of the month.</param>
    /// <param name="today">The day of the hotel.</param>
    /// <param name="size">The one size to show, or <c>null</c> for all of them.</param>
    /// <param name="nights">The figures from the service. A night without figures is shown empty.</param>
    /// <returns>The days from <see cref="FirstShown"/> up to <see cref="EndShown"/>, a multiple of seven.</returns>
    public static IReadOnlyList<CalendarDay> Build(
        DateOnly month,
        DateOnly today,
        RoomSize? size,
        IReadOnlyList<NightAvailabilityDto> nights)
    {
        ArgumentNullException.ThrowIfNull(nights);

        Dictionary<DateOnly, NightAvailabilityDto> byNight = nights.ToDictionary(night => night.Night);
        DateOnly end = EndShown(month);
        List<CalendarDay> days = [];

        for (DateOnly date = FirstShown(month); date < end; date = date.AddDays(1))
        {
            IReadOnlyList<RoomTypeAvailabilityDto> types = byNight.TryGetValue(date, out NightAvailabilityDto? night)
                ? night.Types
                : [];

            CalendarSlot[] slots = types
                .Where(type => size is null || type.Size == size)
                .Select(type => new CalendarSlot(type.Size, type.Free, type.Sellable))
                .ToArray();

            bool inMonth = date.Year == month.Year && date.Month == month.Month;
            days.Add(new CalendarDay(date, inMonth, today, slots, types));
        }

        return days;
    }

    /// <summary>
    /// The nights from today to the end of the month on which a size shown is sold out: the figure the desk
    /// looks at before it promises a room.
    /// </summary>
    /// <param name="days">The days of the grid.</param>
    /// <returns>The number of such nights.</returns>
    public static int SoldOutNightsAhead(IEnumerable<CalendarDay> days)
    {
        ArgumentNullException.ThrowIfNull(days);

        return days.Count(day => day.InMonth && !day.IsPast && day.HasSoldOutSize);
    }

    /// <summary>The colour of a figure: sold out at none, a warning at the last of several rooms.</summary>
    /// <param name="free">The free rooms.</param>
    /// <param name="sellable">The rooms that can be sold.</param>
    /// <returns>The tone.</returns>
    public static CalendarTone ToneOf(int free, int sellable) => (free, sellable) switch
    {
        (_, 0) => CalendarTone.None,
        (0, _) => CalendarTone.Full,
        (1, > 1) => CalendarTone.Low,
        _ => CalendarTone.Ok,
    };

    /// <summary>The class of a tone in app.css.</summary>
    /// <param name="tone">The tone.</param>
    /// <returns>The class name.</returns>
    public static string ClassOf(CalendarTone tone) => tone switch
    {
        CalendarTone.Ok => "ok",
        CalendarTone.Low => "low",
        CalendarTone.Full => "full",
        _ => "none",
    };

    /// <summary>A number of rooms of one size in words, for example "1 suite" or "5 double rooms".</summary>
    /// <param name="size">The room size.</param>
    /// <param name="count">The number of rooms.</param>
    /// <returns>The text.</returns>
    public static string Rooms(RoomSize size, int count)
    {
        string noun = size switch
        {
            RoomSize.Single => count == 1 ? "single room" : "single rooms",
            RoomSize.Double => count == 1 ? "double room" : "double rooms",
            RoomSize.Suite => count == 1 ? "suite" : "suites",
            _ => count == 1 ? "room" : "rooms",
        };

        return string.Concat(count.ToString(CultureInfo.InvariantCulture), " ", noun);
    }
}
