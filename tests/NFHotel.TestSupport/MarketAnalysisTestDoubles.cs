using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;

namespace NFHotel.TestSupport;

/// <summary>An in-memory repository of the stored market analyses.</summary>
public sealed class InMemoryMarketAnalysisRunRepository : InMemoryRepository<MarketAnalysisRun>, IMarketAnalysisRunRepository
{
    public Task<MarketAnalysisRun?> FindByRunIdAsync(Guid runId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(run => run.RunId == runId));

    public Task<MarketAnalysisRun?> FindLatestAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.OrderByDescending(run => run.FinishedAt).ThenByDescending(run => run.Id).FirstOrDefault());
}

/// <summary>
/// A fake of the data analysis service: a start queues a run, and the test moves the run on with
/// <see cref="State"/> and <see cref="Result"/>. It counts the calls, so a test can see that a stored run is not read twice.
/// </summary>
public sealed class FakeMarketAnalysisServiceClient : IMarketAnalysisServiceClient
{
    public static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-0000000000a1");

    /// <summary>The error code every call answers with; <c>null</c> lets the calls work.</summary>
    public string? FailWith { get; set; }

    /// <summary>The state that a read of the run answers with.</summary>
    public string State { get; set; } = MarketAnalysisRunStates.Queued;

    /// <summary>The result that a read of the result answers with.</summary>
    public MarketAnalysisResultDto Result { get; set; } = SampleResult();

    public int StartCount { get; private set; }

    public int ResultReads { get; private set; }

    public static MarketAnalysisResultDto SampleResult(int schemaVersion = 1) =>
        new(
            RunId,
            schemaVersion,
            new DateTimeOffset(2026, 10, 19, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 19, 10, 4, 0, TimeSpan.Zero),
            "ollama",
            "phi3.5:latest",
            new MarketAnalysisContentDto(
                "Portugal is the largest market.\n\nCancellations are highest in the summer.",
                [new MarketAnalysisFigureDto("Cancellation rate", 37.1, "%")],
                [new MarketAnalysisSeriesDto("bookings_by_country", "Bookings by country", "bookings", [new("Portugal", 48590), new("United Kingdom", 12129)])]));

    public Task<Result<MarketAnalysisRunStateDto>> StartRunAsync(CancellationToken cancellationToken = default)
    {
        StartCount++;
        return Task.FromResult(FailWith is { } code
            ? Result<MarketAnalysisRunStateDto>.Failure(code)
            : Result<MarketAnalysisRunStateDto>.Success(new(RunId, MarketAnalysisRunStates.Queued, null, null, null)));
    }

    public Task<Result<MarketAnalysisRunStateDto>> GetRunAsync(Guid runId, CancellationToken cancellationToken = default) =>
        Task.FromResult(
            FailWith is { } code ? Result<MarketAnalysisRunStateDto>.Failure(code)
            : runId != RunId ? Result<MarketAnalysisRunStateDto>.Failure(ErrorCodes.MarketAnalysisRunner.RunNotFound)
            : Result<MarketAnalysisRunStateDto>.Success(new(RunId, State, null, null, State == MarketAnalysisRunStates.Failed ? "notebook_execution_failed" : null)));

    public Task<Result<MarketAnalysisResultDto>> GetResultAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        ResultReads++;
        return Task.FromResult(
            FailWith is { } code ? Result<MarketAnalysisResultDto>.Failure(code)
            : runId != RunId ? Result<MarketAnalysisResultDto>.Failure(ErrorCodes.MarketAnalysisRunner.RunNotFound)
            : Result<MarketAnalysisResultDto>.Success(Result));
    }
}
