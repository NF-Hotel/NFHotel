using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.UseCases;

namespace NFHotel.Application.Ports;

/// <summary>
/// The market analysis service (repository <c>notebook-market-analysis</c>) as the website API calls it: start a run with the
/// bookings, follow it by its run id and read the nine analyses with their AI insights. No HTTP request stays open while the
/// models work, but the call as a whole lasts until the run ends, up to many minutes, so the caller runs it in the background.
/// </summary>
public interface IMarketAnalysisServiceClient
{
    /// <summary>Runs the analyses on a list of bookings, with the AI insights, and waits for the result.</summary>
    /// <param name="request">The bookings, the rooms of the hotel and the period.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>
    /// The result, or a code: not configured, unauthorized, input rejected, already running, unavailable, unexpected response,
    /// or <c>market_analysis_runner.run_failed:&lt;error_code&gt;</c> when the service ended the run as failed.
    /// </returns>
    Task<Result<MarketAnalysisResultDto>> AnalyzeAsync(
        MarketAnalysisRunRequest request,
        CancellationToken cancellationToken = default);
}
