using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases.MarketAnalysis;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>The state of the run that is going on, and the split of a stay into weekend nights and week nights (MIL-026).</summary>
public sealed class MarketAnalysisRunTrackerAndNightsTests
{
    /// <summary>Positive: a run goes queued, running, completed with the times of the clock.</summary>
    [Fact]
    public void Tracker_FullRun_RecordsTheStatesAndTimes()
    {
        // Arrange
        var clock = new FakeClock();
        var tracker = new MarketAnalysisRunTracker(clock);
        var runId = tracker.TryBegin().Value.RunId;

        // Act
        var queued = tracker.Get(runId)!;
        tracker.MarkRunning(runId);
        var started = clock.UtcNow;
        clock.TimeOfDay += TimeSpan.FromMinutes(7);
        tracker.MarkCompleted(runId);
        var done = tracker.Get(runId)!;

        // Assert
        Assert.Equal((MarketAnalysisRunStates.Queued, (DateTimeOffset?)null), (queued.State, queued.StartedAt));
        Assert.Equal((MarketAnalysisRunStates.Completed, started, clock.UtcNow), (done.State, done.StartedAt, done.FinishedAt));
        Assert.True(done.IsFinished);
    }

    /// <summary>Negative: a second begin while a run is not finished is refused; after it finished, or failed, a new one is allowed.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Tracker_Begin_IsRefusedWhileARunIsActive(bool fails)
    {
        // Arrange
        var tracker = new MarketAnalysisRunTracker(new FakeClock());
        var first = tracker.TryBegin().Value.RunId;

        // Act
        var whileActive = tracker.TryBegin();
        tracker.MarkRunning(first);
        var whileRunning = tracker.TryBegin();
        if (fails)
        {
            tracker.MarkFailed(first, ErrorCodes.MarketAnalysisRunner.Unavailable);
        }
        else
        {
            tracker.MarkCompleted(first);
        }

        var afterwards = tracker.TryBegin();

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.AlreadyRunning, Assert.Single(whileActive.Errors).Code);
        Assert.True(whileRunning.IsFailure);
        Assert.True(afterwards.IsSuccess);
        Assert.NotEqual(first, afterwards.Value.RunId);
    }

    /// <summary>Boundary: only the last run is kept, and a mark for another run id changes nothing.</summary>
    [Fact]
    public void Tracker_OtherRunId_IsUnknownAndChangesNothing()
    {
        // Arrange
        var tracker = new MarketAnalysisRunTracker(new FakeClock());
        var runId = tracker.TryBegin().Value.RunId;

        // Act
        tracker.MarkCompleted(Guid.NewGuid());

        // Assert
        Assert.Null(tracker.Get(Guid.NewGuid()));
        Assert.Equal(MarketAnalysisRunStates.Queued, tracker.Get(runId)!.State);
    }

    /// <summary>Negative: a failed run keeps its code.</summary>
    [Fact]
    public void Tracker_Failed_KeepsTheCode()
    {
        // Arrange
        var tracker = new MarketAnalysisRunTracker(new FakeClock());
        var runId = tracker.TryBegin().Value.RunId;

        // Act
        tracker.MarkFailed(runId, ErrorCodes.MarketAnalysis.NoBookings);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysis.NoBookings, tracker.Get(runId)!.Error);
    }

    /// <summary>Positive: a Friday to Monday stay is a Friday night as a week night and Saturday and Sunday nights as weekend nights.</summary>
    [Fact]
    public void Split_FridayToMonday_IsOneWeekNightAndTwoWeekendNights()
    {
        // Act
        var (weekend, week) = StayNights.Split(new DateOnly(2019, 3, 1), new DateOnly(2019, 3, 4));

        // Assert
        Assert.Equal((2, 1), (weekend, week));
    }

    /// <summary>Boundary: one night, a week and an end that is not after the start.</summary>
    [Theory]
    [InlineData(2019, 3, 2, 2019, 3, 3, 1, 0)]
    [InlineData(2019, 3, 4, 2019, 3, 11, 2, 5)]
    [InlineData(2019, 3, 4, 2019, 3, 4, 0, 0)]
    [InlineData(2019, 3, 5, 2019, 3, 4, 0, 0)]
    public void Split_Boundaries_CountTheNightsOfTheHalfOpenPeriod(int y1, int m1, int d1, int y2, int m2, int d2, int weekend, int week)
    {
        // Act and assert
        Assert.Equal((weekend, week), StayNights.Split(new DateOnly(y1, m1, d1), new DateOnly(y2, m2, d2)));
    }
}
