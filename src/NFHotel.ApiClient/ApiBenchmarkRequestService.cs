using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Benchmark;

namespace NFHotel.ApiClient;

/// <summary>
/// Records the benchmark runs a Manager starts, and their states, through the API for the web application (ADR-0031).
/// The API takes the name of the staff member from the staff token, so the <c>requestedBy</c> argument is not sent.
/// </summary>
public sealed class ApiBenchmarkRequestService : IBenchmarkRequestService
{
    private readonly ApiGateway _api;

    /// <summary>Creates the client.</summary>
    /// <param name="api">The gateway that carries the tokens.</param>
    public ApiBenchmarkRequestService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    public Task<Result<BenchmarkRequestDto>> RecordAsync(
        RecordBenchmarkRequest request,
        string requestedBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _api.PostAsync<BenchmarkRequestDto>($"{ApiRoutes.Benchmark}/{ApiRoutes.BenchmarkRequests}", request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<BenchmarkRequestDto>> SetStateAsync(
        Guid runId,
        SetBenchmarkRequestStateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (runId == Guid.Empty)
        {
            return Result<BenchmarkRequestDto>.Failure(ErrorCodes.Benchmark.NotFound);
        }

        return await _api
            .PutAsync<BenchmarkRequestDto>($"{ApiRoutes.Benchmark}/{ApiRoutes.BenchmarkRequests}/{runId}/state", request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<BenchmarkRequestDto>>> GetRecentAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<IReadOnlyList<BenchmarkRequestDto>>($"{ApiRoutes.Benchmark}/{ApiRoutes.BenchmarkRequests}", cancellationToken);
}
