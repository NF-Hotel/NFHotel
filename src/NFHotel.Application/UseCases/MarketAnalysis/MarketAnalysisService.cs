using System.Text.Json;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;

namespace NFHotel.Application.UseCases.MarketAnalysis;

/// <summary>
/// Starts the market analysis on the data analysis service, and stores the result of a completed run once (MIL-026).
/// The service does not push a result, so the first read of a completed run fetches and stores it.
/// </summary>
public sealed class MarketAnalysisService : IMarketAnalysisService
{
    /// <summary>The only result schema version that is read.</summary>
    public const int SupportedSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IMarketAnalysisServiceClient _client;
    private readonly IMarketAnalysisRunRepository _runs;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Creates the service.</summary>
    /// <param name="client">The data analysis service.</param>
    /// <param name="runs">The storage of runs.</param>
    /// <param name="unitOfWork">Saves the changes.</param>
    public MarketAnalysisService(IMarketAnalysisServiceClient client, IMarketAnalysisRunRepository runs, IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _client = client;
        _runs = runs;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public Task<Result<MarketAnalysisRunStateDto>> StartRunAsync(CancellationToken cancellationToken = default) =>
        _client.StartRunAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Result<MarketAnalysisRunStateDto>> GetRunStateAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            return Result<MarketAnalysisRunStateDto>.Failure(ErrorCodes.MarketAnalysisRunner.RunNotFound);
        }

        var state = await _client.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
        if (state.IsFailure || state.Value.State != MarketAnalysisRunStates.Completed)
        {
            return state;
        }

        var stored = await StoreAsync(runId, cancellationToken).ConfigureAwait(false);

        return stored.IsFailure
            ? Result<MarketAnalysisRunStateDto>.Failure(stored.Errors)
            : state;
    }

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

    private async Task<Result> StoreAsync(Guid runId, CancellationToken cancellationToken)
    {
        // A repeat is answered before the service is asked: the page polls, and a run is stored once.
        if (await _runs.FindByRunIdAsync(runId, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Result.Success();
        }

        var result = await _client.GetResultAsync(runId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result.Failure(result.Errors);
        }

        var dto = result.Value;
        if (dto.SchemaVersion != SupportedSchemaVersion)
        {
            return Result.Failure(ErrorCodes.MarketAnalysis.SchemaVersionUnsupported);
        }

        if (dto.Content is null || dto.Content.Figures is null || dto.Content.Series is null)
        {
            return Result.Failure(ErrorCodes.MarketAnalysis.BodyInvalid);
        }

        MarketAnalysisRun run;
        try
        {
            run = MarketAnalysisRun.Create(
                runId,
                dto.SchemaVersion,
                dto.StartedAt,
                dto.FinishedAt,
                dto.Provider,
                dto.Model,
                JsonSerializer.Serialize(dto.Content, JsonOptions));
        }
        catch (DomainException exception)
        {
            return Result.FromCodes(exception.Codes);
        }

        _runs.Add(run);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            // Two polls at the same moment: the other one stored the run first.
        }

        return Result.Success();
    }
}
