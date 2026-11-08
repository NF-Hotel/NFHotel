using NFHotel.Application.Dto.Bookings;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Features.Admin.Bookings;

namespace NFHotel.Web.Tests;

/// <summary>The month grid of the availability calendar: whole weeks, the flags of each day and the colours.</summary>
public sealed class CalendarGridTests
{
    private static readonly DateOnly November = new(2026, 11, 1);

    #region Weeks

    /// <summary>Positive: a month that starts on a Sunday and ends on a Monday is shown from the Monday before to the Sunday after.</summary>
    [Fact]
    public void Shown_MonthStartingOnSunday_CoversWholeWeeks()
    {
        Assert.Equal(new DateOnly(2026, 10, 26), CalendarGrid.FirstShown(November));
        Assert.Equal(new DateOnly(2026, 12, 7), CalendarGrid.EndShown(November));
    }

    /// <summary>Boundary: a month from a Monday to a Sunday is shown without a day around it.</summary>
    [Fact]
    public void Shown_MonthFromMondayToSunday_HasNoDayAroundIt()
    {
        DateOnly february = new(2027, 2, 1);

        Assert.Equal(february, CalendarGrid.FirstShown(february));
        Assert.Equal(new DateOnly(2027, 3, 1), CalendarGrid.EndShown(february));
    }

    /// <summary>Positive: any day of a month gives the 1st of it.</summary>
    [Fact]
    public void MonthOf_AnyDay_GivesTheFirst() =>
        Assert.Equal(November, CalendarGrid.MonthOf(new DateOnly(2026, 11, 30)));

    #endregion

    #region Days

    /// <summary>Positive: the days carry the month, the past and today, and the weekend from the date.</summary>
    [Fact]
    public void Build_Month_FlagsEveryDay()
    {
        DateOnly today = new(2026, 11, 8);

        IReadOnlyList<CalendarDay> days = CalendarGrid.Build(November, today, null, []);

        Assert.Equal(42, days.Count);
        Assert.Equal(new DateOnly(2026, 10, 26), days[0].Date);
        Assert.False(days[0].InMonth);
        Assert.True(days[0].IsPast);
        CalendarDay sunday = days.Single(day => day.Date == today);
        Assert.True(sunday.IsToday);
        Assert.True(sunday.IsWeekend);
        Assert.False(sunday.IsPast);
        Assert.Equal("cal-day weekend is-today", sunday.CssClass);
        Assert.Equal("cal-day weekend is-out", days[^1].CssClass);
    }

    /// <summary>Positive: all sizes are shown without a filter, one size with it, and the totals follow.</summary>
    [Fact]
    public void Build_SizeFilter_ShowsOnlyThatSize()
    {
        NightAvailabilityDto night = Night(November, (RoomSize.Single, 1, 2), (RoomSize.Double, 0, 3), (RoomSize.Suite, 1, 1));

        CalendarDay all = CalendarGrid.Build(November, November, null, [night]).Single(day => day.Date == November);
        CalendarDay doubles = CalendarGrid.Build(November, November, RoomSize.Double, [night]).Single(day => day.Date == November);

        Assert.Equal([RoomSize.Single, RoomSize.Double, RoomSize.Suite], all.Slots.Select(slot => slot.Size).ToArray());
        Assert.Equal((2, 6), (all.Free, all.Sellable));
        Assert.Equal([RoomSize.Double], doubles.Slots.Select(slot => slot.Size).ToArray());
        Assert.Equal((0, 3), (doubles.Free, doubles.Sellable));
        Assert.Equal(CalendarTone.Full, doubles.Tone);
        Assert.Equal(3, doubles.Types.Count);
    }

    /// <summary>Edge: a night the service gave no figures for is shown empty, not as sold out.</summary>
    [Fact]
    public void Build_NightWithoutFigures_IsEmpty()
    {
        CalendarDay day = CalendarGrid.Build(November, November, null, []).Single(candidate => candidate.Date == November);

        Assert.False(day.HasFigures);
        Assert.Empty(day.Slots);
        Assert.False(day.HasSoldOutSize);
        Assert.Equal(0, day.OccupancyPercent);
    }

    /// <summary>Positive: the nights ahead with a size sold out are counted in the month, from today, not before.</summary>
    [Fact]
    public void SoldOutNightsAhead_PastAndOtherMonths_AreLeftOut()
    {
        DateOnly today = new(2026, 11, 8);
        NightAvailabilityDto[] nights =
        [
            Night(new DateOnly(2026, 11, 7), (RoomSize.Double, 0, 2)),
            Night(today, (RoomSize.Double, 0, 2)),
            Night(new DateOnly(2026, 11, 9), (RoomSize.Double, 1, 2)),
            Night(new DateOnly(2026, 11, 30), (RoomSize.Double, 0, 2), (RoomSize.Suite, 0, 0)),
            Night(new DateOnly(2026, 12, 1), (RoomSize.Double, 0, 2)),
        ];

        IReadOnlyList<CalendarDay> days = CalendarGrid.Build(November, today, null, nights);

        Assert.Equal(2, CalendarGrid.SoldOutNightsAhead(days));
    }

    #endregion

    #region Figures

    /// <summary>Equivalence: sold out at none, a warning at the last of several, nothing to count without rooms.</summary>
    [Theory]
    [InlineData(0, 0, CalendarTone.None)]
    [InlineData(0, 3, CalendarTone.Full)]
    [InlineData(1, 3, CalendarTone.Low)]
    [InlineData(1, 1, CalendarTone.Ok)]
    [InlineData(2, 3, CalendarTone.Ok)]
    public void ToneOf_FreeAndSellable_GivesTheColour(int free, int sellable, CalendarTone expected) =>
        Assert.Equal(expected, CalendarGrid.ToneOf(free, sellable));

    /// <summary>Equivalence: each tone has its class in app.css.</summary>
    [Theory]
    [InlineData(CalendarTone.Ok, "ok")]
    [InlineData(CalendarTone.Low, "low")]
    [InlineData(CalendarTone.Full, "full")]
    [InlineData(CalendarTone.None, "none")]
    public void ClassOf_Tone_GivesTheClass(CalendarTone tone, string expected) =>
        Assert.Equal(expected, CalendarGrid.ClassOf(tone));

    /// <summary>Boundary: the bar turns amber at three rooms in four taken and red when every room is.</summary>
    [Theory]
    [InlineData(5, 8, 38, "cal-bar")]
    [InlineData(2, 8, 75, "cal-bar high")]
    [InlineData(0, 8, 100, "cal-bar full")]
    public void Occupancy_TakenRooms_SetsThePercentAndTheBar(int free, int sellable, int percent, string barClass)
    {
        CalendarDay day = CalendarGrid.Build(November, November, null, [Night(November, (RoomSize.Double, free, sellable))])
            .Single(candidate => candidate.Date == November);

        Assert.Equal(percent, day.OccupancyPercent);
        Assert.Equal(barClass, day.BarClass);
    }

    /// <summary>Positive: the tooltip names the free rooms of every size, and says so when none is free.</summary>
    [Fact]
    public void Tooltip_Night_NamesTheFreeRooms()
    {
        NightAvailabilityDto night = new(November,
        [
            new RoomTypeAvailabilityDto(RoomSize.Single, 2, 2, ["101", "104"]),
            new RoomTypeAvailabilityDto(RoomSize.Double, 0, 1, []),
        ]);

        CalendarDay day = CalendarGrid.Build(November, November, null, [night]).Single(candidate => candidate.Date == November);

        Assert.Equal("Night of Sunday 1 November — Single room: 101, 104 · Double room: none free", day.Tooltip);
        Assert.Equal("Sunday 1 November: 2 of 3 rooms free", day.AccessibleLabel);
    }

    /// <summary>Equivalence: a number of rooms is written with the right noun.</summary>
    [Theory]
    [InlineData(RoomSize.Single, 1, "1 single room")]
    [InlineData(RoomSize.Double, 5, "5 double rooms")]
    [InlineData(RoomSize.Suite, 1, "1 suite")]
    [InlineData(RoomSize.Suite, 0, "0 suites")]
    public void Rooms_SizeAndCount_IsWrittenOut(RoomSize size, int count, string expected) =>
        Assert.Equal(expected, CalendarGrid.Rooms(size, count));

    #endregion

    #region Helpers

    private static NightAvailabilityDto Night(DateOnly date, params (RoomSize Size, int Free, int Sellable)[] types) =>
        new(date, types.Select(type => new RoomTypeAvailabilityDto(type.Size, type.Free, type.Sellable, [])).ToArray());

    #endregion
}
