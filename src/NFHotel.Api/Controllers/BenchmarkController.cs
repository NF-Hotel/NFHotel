using Microsoft.AspNetCore.Mvc;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.UseCases.Benchmark;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The benchmark runs (ADR-0013). The benchmark service posts a finished run with the service token only; a Manager reads
/// the stored runs with a staff token that holds <see cref="Permissions.BenchmarkView"/>.
/// </summary>
[ApiController]
[Route(ApiRoutes.Benchmark)]
public sealed class BenchmarkController : ControllerBase
{
    private readonly IBenchmarkIntake _intake;
    private readonly IBenchmarkService _benchmark;

    /// <summary>Creates the controller.</summary>
    /// <param name="intake">Receives a delivered run.</param>
    /// <param name="benchmark">Reads the stored runs.</param>
    public BenchmarkController(IBenchmarkIntake intake, IBenchmarkService benchmark)
    {
        ArgumentNullException.ThrowIfNull(intake);
        ArgumentNullException.ThrowIfNull(benchmark);
        _intake = intake;
        _benchmark = benchmark;
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

    /// <summary>Reads the answer text of one request.</summary>
    /// <param name="id">The key of the request.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The answer, or 404.</returns>
    [HttpGet(ApiRoutes.BenchmarkOutput)]
    [RequiresPermission(Permissions.BenchmarkView)]
    public async Task<IActionResult> GetOutputAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _benchmark.GetOutputAsync(id, cancellationToken).ConfigureAwait(false));
}
