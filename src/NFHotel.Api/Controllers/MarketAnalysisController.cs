using Microsoft.AspNetCore.Mvc;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.UseCases.MarketAnalysis;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The market analysis (MIL-026). The API starts a run on the data analysis service, stores the result of a completed
/// run and serves the latest report. Every endpoint needs a staff token whose role holds the permission.
/// </summary>
[ApiController]
[Route(ApiRoutes.MarketAnalysis)]
public sealed class MarketAnalysisController : ControllerBase
{
    private readonly IMarketAnalysisService _marketAnalysis;

    /// <summary>Creates the controller.</summary>
    /// <param name="marketAnalysis">The use cases of the market analysis.</param>
    public MarketAnalysisController(IMarketAnalysisService marketAnalysis)
    {
        ArgumentNullException.ThrowIfNull(marketAnalysis);
        _marketAnalysis = marketAnalysis;
    }

    /// <summary>Starts a run on the data analysis service.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The queued run, or the codes of why it could not start.</returns>
    [HttpPost(ApiRoutes.MarketAnalysisRuns)]
    [RequiresPermission(Permissions.MarketAnalysisStart)]
    public async Task<IActionResult> StartRunAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _marketAnalysis.StartRunAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Reads the state of a run; a completed run is stored on the first read.</summary>
    /// <param name="runId">The run id from the start.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The state, or the codes.</returns>
    [HttpGet(ApiRoutes.MarketAnalysisRunById)]
    [RequiresPermission(Permissions.MarketAnalysisView)]
    public async Task<IActionResult> GetRunAsync(Guid runId, CancellationToken cancellationToken) =>
        this.ToResponse(await _marketAnalysis.GetRunStateAsync(runId, cancellationToken).ConfigureAwait(false));

    /// <summary>Reads the report of the run that finished last.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The report, or 404 when none is stored.</returns>
    [HttpGet(ApiRoutes.MarketAnalysisLatest)]
    [RequiresPermission(Permissions.MarketAnalysisView)]
    public async Task<IActionResult> GetLatestAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _marketAnalysis.GetLatestReportAsync(cancellationToken).ConfigureAwait(false));
}
