using System.Text.Json;

using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;

namespace NFHotel.Application.UseCases.MarketAnalysis;

/// <summary>
/// The work of one market analysis run (MIL-026): read the bookings of the hotel, send them to the markets API, check the
/// result and store it. The launcher calls it in the background; every outcome is written to the tracker, so nothing is
/// thrown to a caller that is not there.
/// </summary>
public sealed class MarketAnalysisRunner
{
    /// <summary>The only result schema version (the major number) that is read.</summary>
    public const int SupportedSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IMarketAnalysisBookingQueries _bookings;
    private readonly IMarketAnalysisServiceClient _client;
    private readonly IMarketAnalysisRunRepository _runs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MarketAnalysisRunTracker _tracker;
    private readonly IClock _clock;

    /// <summary>Creates the runner.</summary>
    /// <param name="bookings">The read of the hotel bookings.</param>
    /// <param name="client">The markets API.</param>
    /// <param name="runs">The storage of reports.</param>
    /// <param name="unitOfWork">Saves the changes.</param>
    /// <param name="tracker">The state of the run.</param>
    /// <param name="clock">The clock.</param>
    public MarketAnalysisRunner(
        IMarketAnalysisBookingQueries bookings,
        IMarketAnalysisServiceClient client,
        IMarketAnalysisRunRepository runs,
        IUnitOfWork unitOfWork,
        MarketAnalysisRunTracker tracker,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(tracker);
        ArgumentNullException.ThrowIfNull(clock);

        _bookings = bookings;
        _client = client;
        _runs = runs;
        _unitOfWork = unitOfWork;
        _tracker = tracker;
        _clock = clock;
    }

    /// <summary>Runs the analysis and ends the run in the tracker as completed or failed.</summary>
    /// <param name="runId">The id of the run that <see cref="MarketAnalysisRunTracker.TryBegin"/> made.</param>
    /// <param name="cancellationToken">Cancels the run, for example when the API stops.</param>
    /// <returns>A task that ends when the run has ended.</returns>
    public async Task ExecuteAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        _tracker.MarkRunning(runId);
        var startedAt = _clock.UtcNow;

        var bookings = await _bookings.GetBookingsAsync(cancellationToken).ConfigureAwait(false);
        if (bookings.Count == 0)
        {
            _tracker.MarkFailed(runId, ErrorCodes.MarketAnalysis.NoBookings);
            return;
        }

        var result = await _client.AnalyzeAsync(bookings, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            _tracker.MarkFailed(runId, result.Errors[0].Code);
            return;
        }

        var dto = result.Value;
        if (dto.SchemaVersion != SupportedSchemaVersion)
        {
            _tracker.MarkFailed(runId, ErrorCodes.MarketAnalysis.SchemaVersionUnsupported);
            return;
        }

        if (dto.Content is null)
        {
            _tracker.MarkFailed(runId, ErrorCodes.MarketAnalysis.BodyInvalid);
            return;
        }

        MarketAnalysisRun run;
        try
        {
            run = MarketAnalysisRun.Create(
                runId,
                dto.SchemaVersion,
                startedAt,
                _clock.UtcNow,
                dto.Provider,
                dto.Model,
                JsonSerializer.Serialize(dto.Content, JsonOptions));
        }
        catch (DomainException exception)
        {
            _tracker.MarkFailed(runId, exception.Codes[0]);
            return;
        }

        _runs.Add(run);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            // The same run id twice cannot happen with a new id for every start; if it does, the run is already stored.
        }

        _tracker.MarkCompleted(runId);
    }
}
