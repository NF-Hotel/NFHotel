using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Tests.TestSupport;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Bookings;

/// <summary>
/// The limits of a stay, members of <see cref="Booking"/> (MIL-086 decision 3, ADR-0050, BR-N-08, BR-C-07): the
/// arrival is not before today, the stay ends at most five years after today counted in calendar years, and on the
/// public site a stay has at most 30 nights and a party of 1 to 4 guests. The clock is fixed: today is given, never read.
/// </summary>
public class StayLimitTests
{
    private static readonly DateOnly Today = new(2026, 3, 1);

    #region Five years ahead (BR-N-08)

    /// <summary>Boundary: a stay that ends exactly five years after today is accepted; one that ends a day later is refused.</summary>
    [Fact]
    public void CheckPeriod_EndsFiveYearsAhead_IsAcceptedAndADayLaterIsRefused()
    {
        // Arrange
        DateRange lastDay = new(Today.AddDays(10), new DateOnly(2031, 3, 1));
        DateRange dayAfter = new(Today.AddDays(10), new DateOnly(2031, 3, 2));

        // Act & Assert
        Assert.Null(Booking.CheckPeriod(lastDay, Today));
        Assert.Equal(BookingErrors.EndDateTooFarAhead, Booking.CheckPeriod(dayAfter, Today));
    }

    /// <summary>Boundary: an arrival today is accepted, and an arrival yesterday is refused.</summary>
    [Fact]
    public void CheckPeriod_ArrivalTodayOrYesterday_IsAcceptedOrRefused()
    {
        // Arrange
        DateRange fromToday = new(Today, Today.AddDays(2));
        DateRange fromYesterday = new(Today.AddDays(-1), Today.AddDays(2));

        // Act & Assert
        Assert.Null(Booking.CheckPeriod(fromToday, Today));
        Assert.Equal(BookingErrors.StartDateInPast, Booking.CheckPeriod(fromYesterday, Today));
    }

    /// <summary>
    /// Boundary across 29 February: on a leap day the latest departure is 28 February five years later, and from a day
    /// before a leap day the five years end on that day of the month.
    /// </summary>
    [Theory]
    [InlineData("2028-02-29", "2033-02-28", true)]
    [InlineData("2028-02-29", "2033-03-01", false)]
    [InlineData("2027-02-28", "2032-02-28", true)]
    [InlineData("2027-02-28", "2032-02-29", false)]
    [InlineData("2027-03-01", "2032-03-01", true)]
    [InlineData("2027-03-01", "2032-03-02", false)]
    public void CheckPeriod_AcrossTheLeapDay_CountsCalendarYears(string today, string departure, bool accepted)
    {
        // Arrange
        DateOnly day = TestDate.Of(today);
        DateRange period = new(day.AddDays(1), TestDate.Of(departure));

        // Act
        string? error = Booking.CheckPeriod(period, day);

        // Assert
        Assert.Equal(accepted ? null : BookingErrors.EndDateTooFarAhead, error);
    }

    /// <summary>Positive: the latest departure is today five calendar years later, the last day of February on a leap day.</summary>
    [Theory]
    [InlineData("2026-03-01", "2031-03-01")]
    [InlineData("2028-02-29", "2033-02-28")]
    [InlineData("2026-12-31", "2031-12-31")]
    public void LatestDeparture_Today_IsFiveCalendarYearsLater(string today, string expected)
    {
        // Act
        DateOnly latest = Booking.LatestDeparture(TestDate.Of(today));

        // Assert
        Assert.Equal(TestDate.Of(expected), latest);
    }

    #endregion

    #region The public site (BR-C-07)

    /// <summary>Boundary: on the public site 1 and 30 nights are accepted and 31 refused.</summary>
    [Theory]
    [InlineData(1, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    [InlineData(365, false)]
    public void CheckPublicNights_Nights_AcceptsUpToThirty(int nights, bool accepted)
    {
        // Arrange
        DateRange period = new(Today, Today.AddDays(nights));

        // Act
        string? error = Booking.CheckPublicNights(period);

        // Assert
        Assert.Equal(accepted ? null : BookingErrors.StayTooLong, error);
    }

    /// <summary>Boundary: a party of 1 to 4 is accepted; 0, 5 and the ends of the whole numbers are refused.</summary>
    [Theory]
    [InlineData(int.MinValue, false)]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(int.MaxValue, false)]
    public void CheckPublicParty_Guests_AcceptsOneToFour(int guests, bool accepted)
    {
        // Act
        string? error = Booking.CheckPublicParty(guests);

        // Assert
        Assert.Equal(accepted ? null : BookingErrors.GuestsOutOfRange, error);
    }

    /// <summary>
    /// Positive: the latest departure that the public site offers is 30 nights after the arrival, or the five years when
    /// they end first.
    /// </summary>
    [Theory]
    [InlineData("2026-03-10", "2026-04-09")]
    [InlineData("2031-02-20", "2031-03-01")]
    public void LatestPublicDeparture_Arrival_IsThirtyNightsOrTheFiveYears(string arrival, string expected)
    {
        // Act
        DateOnly latest = Booking.LatestPublicDeparture(TestDate.Of(arrival), Today);

        // Assert
        Assert.Equal(TestDate.Of(expected), latest);
    }

    #endregion
}
