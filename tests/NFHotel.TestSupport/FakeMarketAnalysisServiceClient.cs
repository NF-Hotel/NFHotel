using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;

namespace NFHotel.TestSupport;

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

    public MarketAnalysisRunRequest? LastRequest { get; private set; }

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

    public async Task<Result<MarketAnalysisResultDto>> AnalyzeAsync(MarketAnalysisRunRequest request, CancellationToken cancellationToken = default)
    {
        Calls++;
        LastRequest = request;
        LastBookings = request.Bookings;
        if (Gate is { } gate)
        {
            await gate.Task.ConfigureAwait(false);
        }

        return FailWith is { } code
            ? Result<MarketAnalysisResultDto>.Failure(code)
            : Result<MarketAnalysisResultDto>.Success(Result);
    }
}
