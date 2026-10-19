using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>The period of the bookings that a market analysis is made for, chosen by arrival date (MIL-026).</summary>
public sealed class MarketAnalysisPeriodTests
{
    private static readonly DateOnly Arrival = new(2019, 3, 1);

    /// <summary>Negative: a first date after the last date is refused with its code, and nothing is started.</summary>
    [Fact]
    public async Task StartRunAsync_FromAfterTo_IsRefusedAndStartsNothing()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.MarketAnalysisService().StartRunAsync(new StartMarketAnalysisRequest(Arrival.AddDays(1), Arrival), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysis.PeriodInvalid, Assert.Single(result.Errors).Code);
        Assert.Empty(world.MarketAnalysisLauncher.Launched);
    }

    /// <summary>Boundary: one day, an open start and an open end are valid periods.</summary>
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task StartRunAsync_ValidPeriods_AreAccepted(bool hasFrom, bool hasTo)
    {
        // Arrange
        var world = new ServiceWorld();
        var request = new StartMarketAnalysisRequest(hasFrom ? Arrival : null, hasTo ? Arrival : null);

        // Act
        var result = await world.MarketAnalysisService().StartRunAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(request, world.MarketAnalysisTracker.PeriodOf(result.Value.RunId));
    }

    /// <summary>Positive: the run reads only the bookings that arrive in the period, both ends included.</summary>
    [Fact]
    public async Task ExecuteAsync_Period_SendsOnlyTheBookingsOfThePeriod()
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisBookings.Bookings.Clear();
        foreach (var offset in new[] { -1, 0, 5, 10, 11 })
        {
            world.MarketAnalysisBookings.Bookings.Add(FakeMarketAnalysisBookingQueries.Booking(100 + offset) with { ArrivalDate = Arrival.AddDays(offset) });
        }

        var runId = (await world.MarketAnalysisService().StartRunAsync(new StartMarketAnalysisRequest(Arrival, Arrival.AddDays(10)), TestContext.Current.CancellationToken)).Value.RunId;

        // Act
        await world.MarketAnalysisRunner().ExecuteAsync(runId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([100, 105, 110], world.MarketAnalysisClient.LastBookings.Select(booking => booking.BookingId));
        Assert.Equal((Arrival, Arrival.AddDays(10)), world.MarketAnalysisBookings.LastPeriod);
    }

    /// <summary>Negative: a period with no booking fails the run with the no bookings code and does not call the markets API.</summary>
    [Fact]
    public async Task ExecuteAsync_NoBookingInThePeriod_FailsWithoutCallingTheService()
    {
        // Arrange
        var world = new ServiceWorld();
        var runId = (await world.MarketAnalysisService().StartRunAsync(new StartMarketAnalysisRequest(new DateOnly(2030, 1, 1), null), TestContext.Current.CancellationToken)).Value.RunId;

        // Act
        await world.MarketAnalysisRunner().ExecuteAsync(runId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysis.NoBookings, world.MarketAnalysisTracker.Get(runId)!.Error);
        Assert.Equal(0, world.MarketAnalysisClient.Calls);
    }

    /// <summary>State: the period belongs to the run: another run id gets an open period, and a new run replaces the old period.</summary>
    [Fact]
    public async Task Tracker_PeriodOf_BelongsToTheRun()
    {
        // Arrange
        var world = new ServiceWorld();
        var tracker = world.MarketAnalysisTracker;
        var first = tracker.TryBegin(Arrival, null).Value.RunId;

        // Act
        var other = tracker.PeriodOf(Guid.NewGuid());
        tracker.MarkCompleted(first);
        var second = tracker.TryBegin(null, Arrival).Value.RunId;
        await Task.CompletedTask;

        // Assert
        Assert.Equal(new StartMarketAnalysisRequest(), other);
        Assert.Equal(new StartMarketAnalysisRequest(null, Arrival), tracker.PeriodOf(second));
        Assert.Equal(new StartMarketAnalysisRequest(), tracker.PeriodOf(first));
    }
}
