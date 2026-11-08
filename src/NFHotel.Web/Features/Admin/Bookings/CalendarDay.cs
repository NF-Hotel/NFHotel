using System.Globalization;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Bookings;

/// <summary>
/// One day in the month grid of the availability calendar, with the figures of the night that starts on it.
/// </summary>
/// <remarks>
/// Everything the cell shows is worked out here, so the markup only places it and holds no rule of its own.
/// </remarks>
public sealed class CalendarDay
{
    /// <summary>Creates the day.</summary>
    /// <param name="date">The date.</param>
    /// <param name="inMonth">True when the date is in the month shown, not in a week around it.</param>
    /// <param name="today">The day of the hotel, which decides what is past.</param>
    /// <param name="slots">The room sizes the filter shows.</param>
    /// <param name="types">Every room size, for the tooltip, which names the free rooms of all of them.</param>
    public CalendarDay(
        DateOnly date,
        bool inMonth,
        DateOnly today,
        IReadOnlyList<CalendarSlot> slots,
        IReadOnlyList<RoomTypeAvailabilityDto> types)
    {
        ArgumentNullException.ThrowIfNull(slots);
        ArgumentNullException.ThrowIfNull(types);

        Date = date;
        InMonth = inMonth;
        IsPast = date < today;
        IsToday = date == today;
        Slots = slots;
        Types = types;
        Free = slots.Sum(slot => slot.Free);
        Sellable = slots.Sum(slot => slot.Sellable);
    }

    /// <summary>The date the night starts on.</summary>
    public DateOnly Date { get; }

    /// <summary>True when the date is in the month shown.</summary>
    public bool InMonth { get; }

    /// <summary>True before the day of the hotel: shown, but no booking can start on it (BR-44).</summary>
    public bool IsPast { get; }

    /// <summary>True on the day of the hotel.</summary>
    public bool IsToday { get; }

    /// <summary>True on Saturday and Sunday, which the grid shades.</summary>
    public bool IsWeekend => Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    /// <summary>The room sizes the filter shows.</summary>
    public IReadOnlyList<CalendarSlot> Slots { get; }

    /// <summary>Every room size with its free rooms.</summary>
    public IReadOnlyList<RoomTypeAvailabilityDto> Types { get; }

    /// <summary>The free rooms of the sizes shown.</summary>
    public int Free { get; }

    /// <summary>The rooms of the sizes shown that can be sold.</summary>
    public int Sellable { get; }

    /// <summary>True when the service gave figures for the night.</summary>
    public bool HasFigures => Types.Count > 0;

    /// <summary>The colour of the large figure when one size is chosen.</summary>
    public CalendarTone Tone => CalendarGrid.ToneOf(Free, Sellable);

    /// <summary>The class of the large figure.</summary>
    public string ToneClass => CalendarGrid.ClassOf(Tone);

    /// <summary>True when a size that can be sold has no room left.</summary>
    public bool HasSoldOutSize => Slots.Any(slot => slot.Tone == CalendarTone.Full);

    /// <summary>The share of the rooms shown that are taken, in whole per cent.</summary>
    public int OccupancyPercent => Sellable == 0
        ? 0
        : (int)Math.Round((Sellable - Free) * 100d / Sellable, MidpointRounding.AwayFromZero);

    /// <summary>The class of the bar under the day: amber from three in four rooms taken, red when all are.</summary>
    public string BarClass => OccupancyPercent switch
    {
        >= 100 => "cal-bar full",
        >= 75 => "cal-bar high",
        _ => "cal-bar",
    };

    /// <summary>The classes of the cell.</summary>
    public string CssClass
    {
        get
        {
            List<string> classes = ["cal-day"];

            if (IsWeekend)
            {
                classes.Add("weekend");
            }

            if (!InMonth)
            {
                classes.Add("is-out");
            }

            if (IsPast)
            {
                classes.Add("is-past");
            }

            if (IsToday)
            {
                classes.Add("is-today");
            }

            return string.Join(' ', classes);
        }
    }

    /// <summary>The day of the month.</summary>
    public string DayText => Date.Day.ToString(CultureInfo.InvariantCulture);

    /// <summary>The day in words, for example "Sunday 8 November".</summary>
    public string LongText => Date.ToString("dddd d MMMM", CultureInfo.InvariantCulture);

    /// <summary>What a screen reader says for the cell.</summary>
    public string AccessibleLabel => string.Create(
        CultureInfo.InvariantCulture,
        $"{LongText}: {Free} of {Sellable} rooms free");

    /// <summary>The tooltip: the free room numbers of every size, so the desk can answer a call without a click.</summary>
    public string Tooltip
    {
        get
        {
            IEnumerable<string> sizes = Types.Select(type => string.Concat(
                DisplayText.Describe(type.Size),
                ": ",
                type.FreeRooms.Count == 0 ? "none free" : string.Join(", ", type.FreeRooms)));

            return string.Concat("Night of ", LongText, " — ", string.Join(" · ", sizes));
        }
    }
}
