using NFHotel.Application.Dto.MarketAnalysis;
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

/// <summary>The bookings of the hotel as the market analysis reads them; a test fills the list.</summary>
public sealed class FakeMarketAnalysisBookingQueries : IMarketAnalysisBookingQueries
{
    public List<MarketBookingDto> Bookings { get; } = [Booking(1), Booking(2, canceled: true)];

    public static MarketBookingDto Booking(int id, bool canceled = false) =>
        new(id, "NF Hotel", canceled, 30, new DateOnly(2019, 1, 30), new DateOnly(2019, 3, 1), 1, 2, 2, 0, 0, "BB", "Portugal", "Direct", false, 0, "C", 0, "No Deposit", "Contract", 0, 1, 94.00m);

    public (DateOnly? From, DateOnly? To)? LastPeriod { get; private set; }

    public Task<IReadOnlyList<MarketBookingDto>> GetBookingsAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        LastPeriod = (from, to);
        return Task.FromResult<IReadOnlyList<MarketBookingDto>>(
            [.. Bookings.Where(booking => (from is null || booking.ArrivalDate >= from) && (to is null || booking.ArrivalDate <= to))]);
    }
}

/// <summary>
/// A fake of the markets API. It records the bookings it was sent and answers a result or a code; a test can hold a call open
/// with <see cref="Gate"/> to see the run while it is running.
/// </summary>
public sealed class FakeMarketAnalysisServiceClient : IMarketAnalysisServiceClient
{
    /// <summary>The error code every call answers with; <c>null</c> lets the call work.</summary>
    public string? FailWith { get; set; }

    /// <summary>The result that a call answers with.</summary>
    public MarketAnalysisResultDto Result { get; set; } = SampleResult();

    /// <summary>When set, a call waits for it before it answers.</summary>
    public TaskCompletionSource? Gate { get; set; }

    public int Calls { get; private set; }

    public IReadOnlyList<MarketBookingDto> LastBookings { get; private set; } = [];

    public static MarketAnalysisResultDto SampleResult(int schemaVersion = 1) =>
        new(
            Guid.Parse("5b3f0e1a-0000-4000-8000-0000000000a1"),
            schemaVersion,
            new DateTimeOffset(2026, 10, 19, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 19, 10, 4, 0, TimeSpan.Zero),
            "ollama",
            "phi3.5:latest",
            new MarketAnalysisContentDto(
                [new MarketAnalysisFigureDto("Bookings analysed", 2, string.Empty)],
                [
                    new MarketAnalysisSectionDto(
                        "cancellations",
                        "Cancellations",
                        "available",
                        null,
                        [new MarketAnalysisSeriesDto("by_deposit_type", "By deposit type", "%", [new("No Deposit", 27.5), new("Non Refund", 99.4)])],
                        new MarketAnalysisInsightDto(
                            "available",
                            null,
                            "Non-refundable bookings are cancelled most often.",
                            [new MarketAnalysisSuggestionDto("Review the non-refundable rate.", "99.4% of 1,200 bookings", 1200)])),
                    new MarketAnalysisSectionDto("room_value", "Room value and stay", "unavailable", "Missing field: price_per_night", [], null),
                ]));

    public async Task<Result<MarketAnalysisResultDto>> AnalyzeAsync(IReadOnlyList<MarketBookingDto> bookings, CancellationToken cancellationToken = default)
    {
        Calls++;
        LastBookings = bookings;
        if (Gate is { } gate)
        {
            await gate.Task.ConfigureAwait(false);
        }

        return FailWith is { } code
            ? Result<MarketAnalysisResultDto>.Failure(code)
            : Result<MarketAnalysisResultDto>.Success(Result);
    }
}

/// <summary>A launcher that only records the run; the test decides when the work runs, so it can look at every state.</summary>
public sealed class RecordingMarketAnalysisLauncher : IMarketAnalysisRunLauncher
{
    public List<Guid> Launched { get; } = [];

    public void Launch(Guid runId) => Launched.Add(runId);
}
