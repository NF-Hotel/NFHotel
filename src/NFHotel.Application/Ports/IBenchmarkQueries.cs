using NFHotel.Application.Dto.Benchmark;

namespace NFHotel.Application.Ports;

/// <summary>Reads of the stored benchmark runs that return DTOs and never the answer text in a list (ADR-0013).</summary>
public interface IBenchmarkQueries
{
    /// <summary>Lists the runs, newest first.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The runs.</returns>
    Task<IReadOnlyList<BenchmarkRunListItemDto>> GetRunsAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads one run with every request.</summary>
    /// <param name="runId">The run id of the contract.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The run, or <c>null</c>.</returns>
    Task<BenchmarkRunDetailsDto?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>Reads the answer text and the error of one request.</summary>
    /// <param name="benchmarkResultId">The key of the request.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The answer, or <c>null</c> when no such request exists.</returns>
    Task<BenchmarkOutputDto?> GetOutputAsync(int benchmarkResultId, CancellationToken cancellationToken = default);
}
