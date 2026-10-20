using System.Text.Json;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Repositories;

namespace NFHotel.Application.UseCases.MarketAnalysis;

/// <summary>
/// Starts the market analysis in the background, tells the state of the run and serves the latest stored report (MIL-026).
/// The work itself is <see cref="MarketAnalysisRunner"/>.
/// </summary>
public sealed class MarketAnalysisService : IMarketAnalysisService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly MarketAnalysisRunTracker _tracker;
    private readonly IMarketAnalysisRunLauncher _launcher;
    private readonly IMarketAnalysisRunRepository _runs;

    /// <summary>Creates the service.</summary>
    /// <param name="tracker">The state of the run.</param>
    /// <param name="launcher">Starts the work in the background.</param>
    /// <param name="runs">The storage of reports.</param>
    public MarketAnalysisService(MarketAnalysisRunTracker tracker, IMarketAnalysisRunLauncher launcher, IMarketAnalysisRunRepository runs)
    {
        ArgumentNullException.ThrowIfNull(tracker);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(runs);

        _tracker = tracker;
        _launcher = launcher;
        _runs = runs;
    }

    /// <inheritdoc />
    public Task<Result<MarketAnalysisRunStateDto>> StartRunAsync(StartMarketAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.From is { } from && request.To is { } to && from > to)
        {
            return Task.FromResult(Result<MarketAnalysisRunStateDto>.Failure(ErrorCodes.MarketAnalysis.PeriodInvalid));
        }

        var begun = _tracker.TryBegin(request.From, request.To);
        if (begun.IsSuccess)
        {
            _launcher.Launch(begun.Value.RunId);
        }

        return Task.FromResult(begun);
    }

    /// <inheritdoc />
    public Task<Result<MarketAnalysisRunStateDto>> GetRunStateAsync(Guid runId, CancellationToken cancellationToken = default) =>
        Task.FromResult(
            _tracker.Get(runId) is { } state
                ? Result<MarketAnalysisRunStateDto>.Success(state)
                : Result<MarketAnalysisRunStateDto>.Failure(ErrorCodes.MarketAnalysisRunner.RunNotFound));

    /// <inheritdoc />
    public async Task<Result<MarketAnalysisReportDto>> GetLatestReportAsync(CancellationToken cancellationToken = default)
    {
        var run = await _runs.FindLatestAsync(cancellationToken).ConfigureAwait(false);
        if (run is null)
        {
            return Result<MarketAnalysisReportDto>.Failure(ErrorCodes.MarketAnalysis.NotFound);
        }

        var content = ReadContent(run.ContentJson);

        return content is null
            ? Result<MarketAnalysisReportDto>.Failure(ErrorCodes.MarketAnalysis.BodyInvalid)
            : Result<MarketAnalysisReportDto>.Success(
                new MarketAnalysisReportDto(run.RunId, run.StartedAt, run.FinishedAt, run.Provider, run.Model, content));
    }

    private static MarketAnalysisContentDto? ReadContent(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<MarketAnalysisContentDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            // A stored document that cannot be read is reported as an invalid body, never thrown at the page.
            return null;
        }
    }
}
