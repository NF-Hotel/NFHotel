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

    /// <summary>Starts the download of a model into Ollama (ADR-0005 of the benchmark service). It returns at once.</summary>
    /// <param name="name">The model name, for example <c>phi4-mini</c>.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The download, or a code: invalid name, already installed, another download active, unavailable.</returns>
    Task<Result<ModelPullDto>> StartModelPullAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Reads the state of a download.</summary>
    /// <param name="pullId">The id from <see cref="StartModelPullAsync"/>.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The state, or the download not found code.</returns>
    Task<Result<ModelPullDto>> GetModelPullAsync(string pullId, CancellationToken cancellationToken = default);

    /// <summary>Deletes an installed model from Ollama.</summary>
    /// <param name="name">The model name as <see cref="ListModelsAsync"/> gave it.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, or a code: not installed, used by a queued or running benchmark, unavailable.</returns>
    Task<Result> DeleteModelAsync(string name, CancellationToken cancellationToken = default);
}
