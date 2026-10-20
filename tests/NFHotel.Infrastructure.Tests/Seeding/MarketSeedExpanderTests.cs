using NFHotel.Infrastructure.Seeding;

namespace NFHotel.Infrastructure.Tests.Seeding;

/// <summary>Filling the hotel with bookings up to a date by repeating the file back to back (MIL-026).</summary>
public sealed class MarketSeedExpanderTests
{
    private static readonly MarketSeedRow[] TwoMonths =
    [
        MarketSeedPlannerTests.Row(1, 0, 2) with { Arrival = new DateOnly(2019, 1, 10), BookedAt = new DateOnly(2018, 12, 1) },
        MarketSeedPlannerTests.Row(2, 0, 2, canceled: true) with { Arrival = new DateOnly(2019, 2, 20), BookedAt = new DateOnly(2019, 2, 1) },
    ];

    /// <summary>Boundary: with no date the rows are returned as they are.</summary>
    [Fact]
    public void Expand_NoDate_ReturnsTheRowsUnchanged()
    {
        // Act and assert
        Assert.Same(TwoMonths, MarketSeedExpander.Expand(TwoMonths, null));
    }

    /// <summary>Positive: the file covers two months, so copy 1 is two months later and copy 2 four months later, up to the date.</summary>
    [Fact]
    public void Expand_ThroughALaterDate_RepeatsTheFileBackToBack()
    {
        // Act
        var rows = MarketSeedExpander.Expand(TwoMonths, new DateOnly(2019, 6, 30));

        // Assert
        Assert.Equal([new DateOnly(2019, 1, 10), new DateOnly(2019, 2, 20), new DateOnly(2019, 3, 10), new DateOnly(2019, 4, 20), new DateOnly(2019, 5, 10), new DateOnly(2019, 6, 20)], rows.Select(row => row.Arrival));
        Assert.Equal([1, 2, 3, 4, 5, 6], rows.Select(row => row.Number));
        Assert.Equal(new DateOnly(2019, 2, 1), rows[2].BookedAt);
        Assert.Equal(rows[1].IsCanceled, rows[3].IsCanceled);
    }

    /// <summary>Boundary: a booking that would arrive after the date is left out, and the date itself is included.</summary>
    [Fact]
    public void Expand_DateInTheMiddleOfACopy_LeavesOutLaterArrivals()
    {
        // Act
        var rows = MarketSeedExpander.Expand(TwoMonths, new DateOnly(2019, 4, 20));

        // Assert
        Assert.Equal(new DateOnly(2019, 4, 20), rows.Max(row => row.Arrival));
        Assert.Equal(4, rows.Count);
    }

    /// <summary>Negative: a date before the file keeps only the rows that arrive by then, and a date inside the file cuts it.</summary>
    [Fact]
    public void Expand_DateInsideTheFile_CutsTheFileAndMakesNoCopy()
    {
        // Act
        var rows = MarketSeedExpander.Expand(TwoMonths, new DateOnly(2019, 1, 31));

        // Assert
        Assert.Equal([1], rows.Select(row => row.Number));
        Assert.Empty(MarketSeedExpander.Expand(TwoMonths, new DateOnly(2018, 1, 1)));
    }

    /// <summary>State: the booking numbers stay unique across copies, and the plan of the expanded rows keeps the overlap rule.</summary>
    [Fact]
    public void Expand_ManyCopies_KeepsNumbersUniqueAndPlansWithoutOverlap()
    {
        // Act
        var rows = MarketSeedExpander.Expand(TwoMonths, new DateOnly(2020, 12, 31));
        var plan = MarketSeedPlanner.Plan(rows);

        // Assert
        Assert.Equal(rows.Count, rows.Select(row => row.Number).Distinct().Count());
        Assert.Equal(1, plan.RoomCount);
    }
}
