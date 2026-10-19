using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Benchmark;

namespace NFHotel.ApiClient;

/// <summary>
/// Reads the stored benchmark runs through the API for the web application (ADR-0008). It implements only the reads: a
/// run is stored by the benchmark service, never by the web application.
/// </summary>
public sealed class ApiBenchmarkService : IBenchmarkService
{
    private readonly ApiGateway _api;

    /// <summary>Creates the client.</summary>
    /// <param name="api">The gateway that carries the tokens.</param>
    public ApiBenchmarkService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<BenchmarkRunListItemDto>>> GetRunsAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<IReadOnlyList<BenchmarkRunListItemDto>>($"{ApiRoutes.Benchmark}/{ApiRoutes.BenchmarkRuns}", cancellationToken);

    /// <inheritdoc />
    public async Task<Result<BenchmarkRunDetailsDto>> GetRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            return Result<BenchmarkRunDetailsDto>.Failure(ErrorCodes.Benchmark.NotFound);
        }

        return await _api
            .GetAsync<BenchmarkRunDetailsDto>($"{ApiRoutes.Benchmark}/{ApiRoutes.BenchmarkRuns}/{runId}", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<BenchmarkOutputDto>> GetOutputAsync(int benchmarkResultId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(benchmarkResultId);

        return await _api
            .GetAsync<BenchmarkOutputDto>($"{ApiRoutes.Benchmark}/{ApiRoutes.BenchmarkResults}/{benchmarkResultId}/output", cancellationToken)
            .ConfigureAwait(false);
    }
}