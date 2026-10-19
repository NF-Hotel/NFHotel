using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;

namespace NFHotel.Application.UseCases.MarketAnalysis;

/// <summary>
/// Keeps the state of the market analysis run that is going on, or the one that ended last (MIL-026). A run takes minutes and
/// the markets API has no job and poll, so the API runs it in the background and the page asks this tracker. There is one
/// run at a time. The state is in memory: a restart of the API forgets it, and the page then says that the run is unknown; the
/// stored report is not lost.
/// </summary>
public sealed class MarketAnalysisRunTracker
{
    private readonly IClock _clock;
    private readonly object _lock = new();
    private MarketAnalysisRunStateDto? _current;

    /// <summary>Creates the tracker.</summary>
    /// <param name="clock">The clock for the times of a run.</param>
    public MarketAnalysisRunTracker(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    /// <summary>Begins a run when none is going on.</summary>
    /// <returns>The queued run, or <c>market_analysis_runner.already_running</c>.</returns>
    public Result<MarketAnalysisRunStateDto> TryBegin()
    {
        lock (_lock)
        {
            if (_current is { IsFinished: false })
            {
                return Result<MarketAnalysisRunStateDto>.Failure(ErrorCodes.MarketAnalysisRunner.AlreadyRunning);
            }

            _current = new MarketAnalysisRunStateDto(Guid.NewGuid(), MarketAnalysisRunStates.Queued, null, null, null);
            return Result<MarketAnalysisRunStateDto>.Success(_current);
        }
    }

    /// <summary>Reads the state of a run.</summary>
    /// <param name="runId">The run id from <see cref="TryBegin"/>.</param>
    /// <returns>The state, or <c>null</c> when this is not the run that the tracker holds.</returns>
    public MarketAnalysisRunStateDto? Get(Guid runId)
    {
        lock (_lock)
        {
            return _current is { } current && current.RunId == runId ? current : null;
        }
    }

    /// <summary>Marks the run as started.</summary>
    /// <param name="runId">The run id.</param>
    public void MarkRunning(Guid runId) =>
        Change(runId, current => current with { State = MarketAnalysisRunStates.Running, StartedAt = _clock.UtcNow });

    /// <summary>Marks the run as completed, with its result stored.</summary>
    /// <param name="runId">The run id.</param>
    public void MarkCompleted(Guid runId) =>
        Change(runId, current => current with { State = MarketAnalysisRunStates.Completed, FinishedAt = _clock.UtcNow });

    /// <summary>Marks the run as failed.</summary>
    /// <param name="runId">The run id.</param>
    /// <param name="code">The error code that says why; it is shown to the Manager as text.</param>
    public void MarkFailed(Guid runId, string code) =>
        Change(runId, current => current with { State = MarketAnalysisRunStates.Failed, FinishedAt = _clock.UtcNow, Error = code });

    private void Change(Guid runId, Func<MarketAnalysisRunStateDto, MarketAnalysisRunStateDto> change)
    {
        lock (_lock)
        {
            if (_current is { } current && current.RunId == runId)
            {
                _current = change(current);
            }
        }
    }
}
