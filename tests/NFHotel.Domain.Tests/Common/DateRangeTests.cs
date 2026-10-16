using NFHotel.Domain.Errors;
using NFHotel.Domain.Tests.TestSupport;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Common;

/// <summary>
/// Tests for <see cref="DateRange"/> — det halvåbne interval [Start, End).
/// Dækker BR-38, BR-43, BR-88, BR-101, BR-102, BR-103, BR-109, BR-117 og BR-39.
/// </summary>
public class DateRangeTests
{
    // --------------------------------------------------------
    // KONSTRUKTØRENS INVARIANTER (BR-101, BR-102, BR-103)
    // --------------------------------------------------------

    /// <summary>BR-101: ankomstdatoen må ikke være <c>default</c>.</summary>
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

    /// <summary>BR-102: afrejsedatoen må ikke være <c>default</c>.</summary>
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
    /// BR-38, BR-43, BR-88, BR-103: afrejse skal være strengt efter ankomst — 0 nætter
    /// er ikke tilladt, og en negativ periode heller ikke.
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

    /// <summary>BR-103: én nat er den korteste lovlige periode.</summary>
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

    // --------------------------------------------------------
    // ANTAL NÆTTER (BR-109, BR-117)
    // --------------------------------------------------------

    /// <summary>
    /// BR-109, BR-117: varigheden er antal overnatninger, ikke antal berørte kalenderdage.
    /// Månedsskifte og skudår må ikke ændre regnestykket.
    /// </summary>
    [Theory]
    [InlineData("2026-01-01", "2026-01-02", 1)]
    [InlineData("2026-01-01", "2026-01-10", 9)]
    [InlineData("2026-01-28", "2026-02-03", 6)]
    [InlineData("2026-12-30", "2027-01-02", 3)]
    [InlineData("2028-02-27", "2028-03-01", 3)]
    public void Nights_ReturnsNumberOfOvernightStays(string start, string end, int expectedNights)
    {
        // Arrange
        var period = new DateRange(TestDate.Of(start), TestDate.Of(end));

        // Act
        var nights = period.Nights;

        // Assert
        Assert.Equal(expectedNights, nights);
    }

    // --------------------------------------------------------
    // OVERLAP (BR-39, B-03)
    // --------------------------------------------------------

    /// <summary>
    /// BR-39: to perioder overlapper kun hvis de deler mindst én nat. Afrejse- og
    /// ankomstdag samme dag er derfor IKKE overlap — i begge retninger.
    /// </summary>
    [Theory]
    // Afrejsedag == ankomstdag: intervallet er halvåbent, så rummet kan genudlejes samme dag.
    [InlineData("2026-01-01", "2026-01-05", "2026-01-05", "2026-01-08", false)]
    [InlineData("2026-01-05", "2026-01-08", "2026-01-01", "2026-01-05", false)]
    // Helt adskilte perioder.
    [InlineData("2026-01-01", "2026-01-05", "2026-01-10", "2026-01-12", false)]
    // Én delt nat i hver ende.
    [InlineData("2026-01-01", "2026-01-05", "2026-01-04", "2026-01-08", true)]
    [InlineData("2026-01-04", "2026-01-08", "2026-01-01", "2026-01-05", true)]
    // Identiske perioder.
    [InlineData("2026-01-01", "2026-01-05", "2026-01-01", "2026-01-05", true)]
    // Den ene indeholder den anden.
    [InlineData("2026-01-01", "2026-01-31", "2026-01-10", "2026-01-12", true)]
    [InlineData("2026-01-10", "2026-01-12", "2026-01-01", "2026-01-31", true)]
    // Samme ankomstdag, forskellig længde.
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
    /// BR-39: overlap er symmetrisk. Ville det ikke være det, ville udfaldet afhænge af
    /// hvilken booking der tilfældigvis blev hentet først.
    /// </summary>
    [Theory]
    [InlineData("2026-01-01", "2026-01-05", "2026-01-05", "2026-01-08")]
    [InlineData("2026-01-01", "2026-01-05", "2026-01-04", "2026-01-08")]
    [InlineData("2026-01-01", "2026-01-31", "2026-01-10", "2026-01-12")]
    public void Overlaps_IsSymmetric(string startA, string endA, string startB, string endB)
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

    // --------------------------------------------------------
    // INDEHOLDER OG FREMTID
    // --------------------------------------------------------

    /// <summary>
    /// Perioden indeholder ankomstdagen, men ikke afrejsedagen — samme halvåbne regel
    /// som overlapstesten (BR-39).
    /// </summary>
    [Theory]
    [InlineData("2025-12-31", false)]
    [InlineData("2026-01-01", true)]
    [InlineData("2026-01-04", true)]
    [InlineData("2026-01-05", false)]
    [InlineData("2026-01-06", false)]
    public void Contains_DepartureDayIsExcluded(string date, bool expected)
    {
        // Arrange
        var period = new DateRange(TestDate.Of("2026-01-01"), TestDate.Of("2026-01-05"));

        // Act
        var contains = period.Contains(TestDate.Of(date));

        // Assert
        Assert.Equal(expected, contains);
    }

    /// <summary>
    /// BR-44, BR-104: en periode må starte i dag, men ikke i går. Datodelen sammenlignes,
    /// og "i dag" leveres udefra (A-05).
    /// </summary>
    [Theory]
    [InlineData("2026-01-01", true)]
    [InlineData("2025-12-31", true)]
    [InlineData("2026-01-02", false)]
    public void StartsOnOrAfter_TodayIsAllowedButYesterdayIsNot(string today, bool expected)
    {
        // Arrange
        var period = new DateRange(TestDate.Of("2026-01-01"), TestDate.Of("2026-01-10"));

        // Act
        var startsOnOrAfter = period.StartsOnOrAfter(TestDate.Of(today));

        // Assert
        Assert.Equal(expected, startsOnOrAfter);
    }
}