using NFHotel.Application.Dto.Benchmark;

namespace NFHotel.Application.UseCases.Benchmark;

/// <summary>
/// Reads the stored benchmark runs for a Manager (ADR-0008). The API implements it with the read port, and the web
/// application with a client over HTTP. Storing a run is <see cref="IBenchmarkIntake"/>, which only the API implements.
/// </summary>
public interface IBenchmarkService
{
    /// <summary>Lists the stored runs, newest first, without any answer text.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The runs.</returns>
    Task<Result<IReadOnlyList<BenchmarkRunListItemDto>>> GetRunsAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads one run with every request and its measurements.</summary>
    /// <param name="runId">The run id of the contract.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The run, or the not found code.</returns>
    Task<Result<BenchmarkRunDetailsDto>> GetRunAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>Reads the answer text and the error of one request.</summary>
    /// <param name="benchmarkResultId">The key of the request.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The answer, or the not found code.</returns>
    Task<Result<BenchmarkOutputDto>> GetOutputAsync(int benchmarkResultId, CancellationToken cancellationToken = default);
}