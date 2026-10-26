using Microsoft.AspNetCore.Mvc;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.UseCases.Benchmark;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The benchmark runs (ADR-0013). The benchmark service posts a finished run with the service token only; a Manager reads
/// the stored runs with a staff token that holds <see cref="Permissions.BenchmarkView"/>. The web application records
/// the runs a Manager starts, and their states, with a staff token that holds <see cref="Permissions.BenchmarkRun"/>
/// (ADR-0031).
/// </summary>
[ApiController]
[Route(ApiRoutes.Benchmark)]
public sealed class BenchmarkController : ControllerBase
{
    private readonly IBenchmarkIntake _intake;
    private readonly IBenchmarkService _benchmark;
    private readonly IBenchmarkRequestService _requests;

    /// <summary>Creates the controller.</summary>
    /// <param name="intake">Receives a delivered run.</param>
    /// <param name="benchmark">Reads the stored runs.</param>
    /// <param name="requests">Records the started runs.</param>
    public BenchmarkController(IBenchmarkIntake intake, IBenchmarkService benchmark, IBenchmarkRequestService requests)
    {
        ArgumentNullException.ThrowIfNull(intake);
        ArgumentNullException.ThrowIfNull(benchmark);
        ArgumentNullException.ThrowIfNull(requests);
        _intake = intake;
        _benchmark = benchmark;
        _requests = requests;
    }

    /// <summary>
    /// Receives a finished run from the benchmark service: 201 for a new run id, 200 for a repeat, 400 with the codes when
    /// the body does not match the contract. The caller is the benchmark service, not a user, so no staff token is read.
    /// </summary>
    /// <param name="request">The body of the contract.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The status code and the envelope.</returns>
    [HttpPost(ApiRoutes.BenchmarkResults)]
    public async Task<IActionResult> PostResultAsync(
        [FromBody] SubmitBenchmarkRunRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _intake.StoreAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status400BadRequest, new ApiResponse(result.Errors.Select(error => error.Code).ToArray()));
        }

        return StatusCode(
            result.Value == BenchmarkStoreOutcome.Created ? StatusCodes.Status201Created : StatusCodes.Status200OK,
            new ApiResponse([]));
    }

    /// <summary>Lists the stored runs, newest first.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The runs.</returns>
    [HttpGet(ApiRoutes.BenchmarkRuns)]
    [RequiresPermission(Permissions.BenchmarkView)]
    public async Task<IActionResult> GetRunsAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _benchmark.GetRunsAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Reads one stored run.</summary>
    /// <param name="runId">The run id of the contract.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The run, or 404.</returns>
    [HttpGet(ApiRoutes.BenchmarkRunById)]
    [RequiresPermission(Permissions.BenchmarkView)]
    public async Task<IActionResult> GetRunAsync(Guid runId, CancellationToken cancellationToken) =>
        this.ToResponse(await _benchmark.GetRunAsync(runId, cancellationToken).ConfigureAwait(false));

    /// <summary>Deletes one stored run with every request and answer of it, for good.</summary>
    /// <param name="runId">The run id of the contract.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>200, or 404 when the run is not stored.</returns>
    [HttpDelete(ApiRoutes.BenchmarkRunById)]
    [RequiresPermission(Permissions.BenchmarkDelete)]
    public async Task<IActionResult> DeleteRunAsync(Guid runId, CancellationToken cancellationToken) =>
        this.ToResponse(await _benchmark.DeleteRunAsync(runId, cancellationToken).ConfigureAwait(false));

    /// <summary>Lists the started runs of the last days that are not done, newest first.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The records.</returns>
    [HttpGet(ApiRoutes.BenchmarkRequests)]
    [RequiresPermission(Permissions.BenchmarkView)]
    public async Task<IActionResult> GetRequestsAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _requests.GetRecentAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Records a run the benchmark service has just accepted; the same run id again changes nothing.</summary>
    /// <param name="request">The run id and the choice.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The record, or the codes that say what is wrong.</returns>
    [HttpPost(ApiRoutes.BenchmarkRequests)]
    [RequiresPermission(Permissions.BenchmarkRun)]
    public async Task<IActionResult> RecordRequestAsync(
        [FromBody] RecordBenchmarkRequest request,
        CancellationToken cancellationToken) =>
        this.ToResponse(
            await _requests
                .RecordAsync(request, User.FindFirst(JwtStaffTokenIssuer.NameClaim)?.Value ?? string.Empty, cancellationToken)
                .ConfigureAwait(false));

    /// <summary>Records the state the benchmark service reported for a run.</summary>
    /// <param name="runId">The run id of the service.</param>
    /// <param name="request">The state and the error.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The record after the change, 404 for an unknown run, or 422 for a refused change.</returns>
    [HttpPut(ApiRoutes.BenchmarkRequestState)]
    [RequiresPermission(Permissions.BenchmarkRun)]
    public async Task<IActionResult> SetRequestStateAsync(
        Guid runId,
        [FromBody] SetBenchmarkRequestStateRequest request,
        CancellationToken cancellationToken) =>
        this.ToResponse(await _requests.SetStateAsync(runId, request, cancellationToken).ConfigureAwait(false));

    /// <summary>Reads the answer text of one request.</summary>
    /// <param name="id">The key of the request.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The answer, or 404.</returns>
    [HttpGet(ApiRoutes.BenchmarkOutput)]
    [RequiresPermission(Permissions.BenchmarkView)]
    public async Task<IActionResult> GetOutputAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _benchmark.GetOutputAsync(id, cancellationToken).ConfigureAwait(false));
}
