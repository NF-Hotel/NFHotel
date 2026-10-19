using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Application.UseCases.MarketAnalysis;

/// <summary>The market analysis as the back office uses it (MIL-026): start a run, follow it and read the latest report.</summary>
public interface IMarketAnalysisService
{
    /// <summary>Starts a run in the background for the bookings of a period.</summary>
    /// <param name="request">The period by arrival date; both dates optional.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The queued run, or a code when the period is wrong or a run is active.</returns>
    Task<Result<MarketAnalysisRunStateDto>> StartRunAsync(StartMarketAnalysisRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the state of a run. When the run has completed and its result is not stored yet, the result is read from the
    /// service and stored before the state is returned.
    /// </summary>
    /// <param name="runId">The run id from <see cref="StartRunAsync"/>.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The state, or a code.</returns>
    Task<Result<MarketAnalysisRunStateDto>> GetRunStateAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>Reads the report of the run that finished last.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The report, or <c>market_analysis.not_found</c> when no run has been stored yet.</returns>
    Task<Result<MarketAnalysisReportDto>> GetLatestReportAsync(CancellationToken cancellationToken = default);
}
