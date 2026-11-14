using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
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
        FakeClock clock = new FakeClock();
        MarketAnalysisRunTracker tracker = new MarketAnalysisRunTracker(clock);
        Guid runId = tracker.TryBegin().Value.RunId;

        // Act
        // The tracker keeps every run it began, so Get finds this one.
        MarketAnalysisRunStateDto queued = tracker.Get(runId)!;
        tracker.MarkRunning(runId);
        DateTimeOffset started = clock.UtcNow;
        clock.TimeOfDay += TimeSpan.FromMinutes(7);
        tracker.MarkCompleted(runId);
        // The tracker keeps every run it began, so Get finds this one.
        MarketAnalysisRunStateDto done = tracker.Get(runId)!;

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
        MarketAnalysisRunTracker tracker = new MarketAnalysisRunTracker(new FakeClock());
        Guid first = tracker.TryBegin().Value.RunId;

        // Act
        Result<MarketAnalysisRunStateDto> whileActive = tracker.TryBegin();
        tracker.MarkRunning(first);
        Result<MarketAnalysisRunStateDto> whileRunning = tracker.TryBegin();
        if (fails)
        {
            tracker.MarkFailed(first, ErrorCodes.MarketAnalysisRunner.Unavailable);
        }
        else
        {
            tracker.MarkCompleted(first);
        }

        Result<MarketAnalysisRunStateDto> afterwards = tracker.TryBegin();

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
        MarketAnalysisRunTracker tracker = new MarketAnalysisRunTracker(new FakeClock());
        Guid runId = tracker.TryBegin().Value.RunId;

        // Act
        tracker.MarkCompleted(Guid.NewGuid());

        // Assert
        Assert.Null(tracker.Get(Guid.NewGuid()));
        // The tracker keeps every run it began, so Get finds this one.
        Assert.Equal(MarketAnalysisRunStates.Queued, tracker.Get(runId)!.State);
    }

    /// <summary>State: the current run is none before a begin, the run that goes on, and the run that ended last.</summary>
    [Fact]
    public void Tracker_Current_IsNoneThenTheRunThatGoesOnThenTheOneThatEnded()
    {
        // Arrange
        MarketAnalysisRunTracker tracker = new MarketAnalysisRunTracker(new FakeClock());
        MarketAnalysisRunStateDto? none = tracker.Current;
        Guid runId = tracker.TryBegin().Value.RunId;
        tracker.MarkRunning(runId);

        // Act
        MarketAnalysisRunStateDto? running = tracker.Current;
        tracker.MarkCompleted(runId);
        MarketAnalysisRunStateDto? ended = tracker.Current;

        // Assert
        Assert.Null(none);
        Assert.NotNull(running);
        Assert.Equal((runId, MarketAnalysisRunStates.Running), (running.RunId, running.State));
        Assert.NotNull(ended);
        Assert.Equal((runId, MarketAnalysisRunStates.Completed), (ended.RunId, ended.State));
    }

    /// <summary>Negative: a failed run keeps its code.</summary>
    [Fact]
    public void Tracker_Failed_KeepsTheCode()
    {
        // Arrange
        MarketAnalysisRunTracker tracker = new MarketAnalysisRunTracker(new FakeClock());
        Guid runId = tracker.TryBegin().Value.RunId;

        // Act
        tracker.MarkFailed(runId, ErrorCodes.MarketAnalysis.NoBookings);

        // Assert
        // The tracker keeps every run it began, so Get finds this one.
        Assert.Equal(ErrorCodes.MarketAnalysis.NoBookings, tracker.Get(runId)!.Error);
    }

    /// <summary>Positive: a Friday to Monday stay is a Friday night as a week night and Saturday and Sunday nights as weekend nights.</summary>
    [Fact]
    public void Split_FridayToMonday_IsOneWeekNightAndTwoWeekendNights()
    {
        // Act
        (int weekend, int week) = StayNights.Split(new DateOnly(2019, 3, 1), new DateOnly(2019, 3, 4));

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
