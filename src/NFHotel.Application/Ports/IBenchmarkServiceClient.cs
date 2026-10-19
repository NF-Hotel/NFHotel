using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.UseCases;

namespace NFHotel.Application.Ports;

/// <summary>
/// The benchmark service as the web application calls it: which models can be run, start a run, and the state of a run
/// (ADR-0001 of the benchmark service). A run takes minutes, so a start returns at once with a run id.
/// </summary>
public interface IBenchmarkServiceClient
{
    /// <summary>Lists the models that are installed and may be run.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The model names, or a code when the service cannot be used.</returns>
    Task<Result<IReadOnlyList<string>>> ListModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>Queues a run.</summary>
    /// <param name="request">The models and options.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The queued run, or a code: model not installed, invalid request, queue full, unavailable.</returns>
    Task<Result<BenchmarkRunStateDto>> StartRunAsync(
        StartBenchmarkRunRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Reads the state of a run.</summary>
    /// <param name="runId">The run id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The state, or the run not found code.</returns>
    Task<Result<BenchmarkRunStateDto>> GetRunAsync(Guid runId, CancellationToken cancellationToken = default);
}
