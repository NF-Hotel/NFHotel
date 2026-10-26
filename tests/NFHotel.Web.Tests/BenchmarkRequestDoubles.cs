using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Benchmark;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>Fakes of the records of started benchmark runs (ADR-0031), for the view models of the benchmark pages.</summary>
public static class BenchmarkRequestDoubles
{
    /// <summary>A record service that knows the given runs, records every new run and accepts every state.</summary>
    /// <param name="recent">The runs it lists.</param>
    /// <returns>The substitute.</returns>
    public static IBenchmarkRequestService Requests(params BenchmarkRequestDto[] recent)
    {
        var requests = Substitute.For<IBenchmarkRequestService>();
        requests.GetRecentAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<BenchmarkRequestDto>>.Success(recent));
        requests.RecordAsync(Arg.Any<RecordBenchmarkRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<BenchmarkRequestDto>.Success(Record(call.Arg<RecordBenchmarkRequest>().RunId, BenchmarkRunStates.Queued)));
        requests.SetStateAsync(Arg.Any<Guid>(), Arg.Any<SetBenchmarkRequestStateRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<BenchmarkRequestDto>.Success(Record(call.Arg<Guid>(), call.Arg<SetBenchmarkRequestStateRequest>().State)));
        return requests;
    }

    /// <summary>A record of a run of the models m1 and m2, with the hotel analysis and two repetitions.</summary>
    /// <param name="runId">The run id.</param>
    /// <param name="state">The state.</param>
    /// <param name="error">The error, or <c>null</c>.</param>
    /// <returns>The record.</returns>
    public static BenchmarkRequestDto Record(Guid runId, string state, string? error = null) =>
        new(
            runId,
            ["m1", "m2"],
            HotelAnalysis: true,
            DeterministicTasks: false,
            Runs: 2,
            RequestedBy: "mona",
            RequestedAt: new DateTimeOffset(2026, 10, 26, 0, 9, 0, TimeSpan.Zero),
            state,
            error,
            UpdatedAt: new DateTimeOffset(2026, 10, 26, 0, 9, 0, TimeSpan.Zero));
}
