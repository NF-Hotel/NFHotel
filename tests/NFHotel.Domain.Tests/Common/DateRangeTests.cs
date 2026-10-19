using NFHotel.Domain.Errors;
using NFHotel.Domain.Tests.TestSupport;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Common;

/// <summary>
/// Tests for <see cref="DateRange"/> — the half-open interval [Start, End).
/// Covers BR-38, BR-43, BR-88, BR-101, BR-102, BR-103, BR-109, BR-117 and BR-39.
/// </summary>
public class DateRangeTests
{
    #region Invariants of the constructor (BR-101, BR-102, BR-103)

    /// <summary>BR-101: the arrival date must not be <c>default</c>.</summary>
    [Fact]
    public void Constructor_StartIsDefault_ThrowsStartRequired()
    {
        // Arrange
        var end = TestDate.Of("2026-01-10");

        // Act
        var exception = Assert.Throws<DomainException>(() => new DateRange(default, end));

        // Assert
        Assert.Equal(DateRange.StartRequired, exception.Code);
    }

    /// <summary>BR-102: the departure date must not be <c>default</c>.</summary>
    [Fact]
    public void Constructor_EndIsDefault_ThrowsEndRequired()
    {
        // Arrange
        var start = TestDate.Of("2026-01-01");

        // Act
        var exception = Assert.Throws<DomainException>(() => new DateRange(start, default));

        // Assert
        Assert.Equal(DateRange.EndRequired, exception.Code);
    }

    /// <summary>
    /// BR-38, BR-43, BR-88, BR-103: departure must be strictly after arrival — 0 nights
    /// is not allowed, and neither is a negative period.
    /// </summary>
    [Theory]
    [InlineData("2026-01-01", "2026-01-01")]
    [InlineData("2026-01-10", "2026-01-01")]
    [InlineData("2026-03-02", "2026-03-01")]
    public void Constructor_EndIsNotAfterStart_ThrowsEndNotAfterStart(string start, string end)
    {
        // Arrange
        var startDate = TestDate.Of(start);
        var endDate = TestDate.Of(end);

        // Act
        var exception = Assert.Throws<DomainException>(() => new DateRange(startDate, endDate));

        // Assert
        Assert.Equal(DateRange.EndNotAfterStart, exception.Code);
    }

    /// <summary>BR-103: one night is the shortest legal period.</summary>
    [Fact]
    public void Constructor_OneNight_IsAccepted()
    {
        // Arrange
        var start = TestDate.Of("2026-01-01");
        var end = TestDate.Of("2026-01-02");

        // Act
        var period = new DateRange(start, end);

        // Assert
        Assert.Equal(1, period.Nights);
    }

    #endregion

    #region Number of nights (BR-109, BR-117)

    /// <summary>
    /// BR-109, BR-117: the duration is the number of overnight stays, not the number of calendar days touched.
    /// A change of month and leap years must not change the calculation.
    /// </summary>
    [Theory]
    [InlineData("2026-01-01", "2026-01-02", 1)]
    [InlineData("2026-01-01", "2026-01-10", 9)]
    [InlineData("2026-01-28", "2026-02-03", 6)]
    [InlineData("2026-12-30", "2027-01-02", 3)]
    [InlineData("2028-02-27", "2028-03-01", 3)]
    public void Nights_AnyPeriod_ReturnsNumberOfOvernightStays(string start, string end, int expectedNights)
    {
        // Arrange
        var period = new DateRange(TestDate.Of(start), TestDate.Of(end));

        // Act
        var nights = period.Nights;

        // Assert
        Assert.Equal(expectedNights, nights);
    }

    #endregion

    #region Overlap (BR-39, B-03)

    /// <summary>
    /// BR-39: two periods overlap only if they share at least one night. Departure and
    /// arrival on the same day is therefore NOT an overlap — in both directions.
    /// </summary>
    [Theory]
    // Departure day == arrival day: the interval is half-open, so the room can be re-let the same day.
    [InlineData("2026-01-01", "2026-01-05", "2026-01-05", "2026-01-08", false)]
    [InlineData("2026-01-05", "2026-01-08", "2026-01-01", "2026-01-05", false)]
    // Completely separate periods.
    [InlineData("2026-01-01", "2026-01-05", "2026-01-10", "2026-01-12", false)]
    // One shared night at each end.
    [InlineData("2026-01-01", "2026-01-05", "2026-01-04", "2026-01-08", true)]
    [InlineData("2026-01-04", "2026-01-08", "2026-01-01", "2026-01-05", true)]
    // Identiske perioder.
    [InlineData("2026-01-01", "2026-01-05", "2026-01-01", "2026-01-05", true)]
    // One contains the other.
    [InlineData("2026-01-01", "2026-01-31", "2026-01-10", "2026-01-12", true)]
    [InlineData("2026-01-10", "2026-01-12", "2026-01-01", "2026-01-31", true)]
    // Same arrival day, different length.
    [InlineData("2026-01-01", "2026-01-03", "2026-01-01", "2026-01-20", true)]
    public void Overlaps_HalfOpenInterval_SharesAtLeastOneNight(
        string startA,
        string endA,
        string startB,
        string endB,
        bool expected)
    {
        // Arrange
        var a = new DateRange(TestDate.Of(startA), TestDate.Of(endA));
        var b = new DateRange(TestDate.Of(startB), TestDate.Of(endB));

        // Act
        var overlaps = a.Overlaps(b);

        // Assert
        Assert.Equal(expected, overlaps);
    }

    /// <summary>
    /// BR-39: overlap is symmetric. If it were not, the outcome would depend on
    /// which booking happened to be fetched first.
    /// </summary>
    [Theory]
    [InlineData("2026-01-01", "2026-01-05", "2026-01-05", "2026-01-08")]
    [InlineData("2026-01-01", "2026-01-05", "2026-01-04", "2026-01-08")]
    [InlineData("2026-01-01", "2026-01-31", "2026-01-10", "2026-01-12")]
    public void Overlaps_EitherOrder_IsSymmetric(string startA, string endA, string startB, string endB)
    {
        // Arrange
        var a = new DateRange(TestDate.Of(startA), TestDate.Of(endA));
        var b = new DateRange(TestDate.Of(startB), TestDate.Of(endB));

        // Act
        var forward = a.Overlaps(b);
        var backward = b.Overlaps(a);

        // Assert
        Assert.Equal(forward, backward);
    }

    #endregion

    #region Contains and future

    /// <summary>
    /// The period contains the arrival day, but not the departure day — the same half-open rule
    /// as the overlap test (BR-39).
    /// </summary>
    [Theory]
    [InlineData("2025-12-31", false)]
    [InlineData("2026-01-01", true)]
    [InlineData("2026-01-04", true)]
    [InlineData("2026-01-05", false)]
    [InlineData("2026-01-06", false)]
    public void Contains_DepartureDay_IsExcluded(string date, bool expected)
    {
        // Arrange
        var period = new DateRange(TestDate.Of("2026-01-01"), TestDate.Of("2026-01-05"));

        // Act
        var contains = period.Contains(TestDate.Of(date));

        // Assert
        Assert.Equal(expected, contains);
    }

    /// <summary>
    /// BR-44, BR-104: a period may start today, but not yesterday. The date part is compared,
    /// and "today" is supplied from outside (A-05).
    /// </summary>
    [Theory]
    [InlineData("2026-01-01", true)]
    [InlineData("2025-12-31", true)]
    [InlineData("2026-01-02", false)]
    public void StartsOnOrAfter_TodayAndYesterday_TodayIsAllowedButYesterdayIsNot(string today, bool expected)
    {
        // Arrange
        var period = new DateRange(TestDate.Of("2026-01-01"), TestDate.Of("2026-01-10"));

        // Act
        var startsOnOrAfter = period.StartsOnOrAfter(TestDate.Of(today));

        // Assert
        Assert.Equal(expected, startsOnOrAfter);
    }

    #endregion
}
