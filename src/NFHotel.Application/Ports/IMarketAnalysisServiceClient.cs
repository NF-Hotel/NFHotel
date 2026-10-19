using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.UseCases;

namespace NFHotel.Application.Ports;

/// <summary>
/// The data analysis service (repository <c>notebook-data-analysis</c>) as the website API calls it: start the
/// <c>market_analysis</c> notebook, read the state of a run and read its result. A run takes minutes, so a start
/// returns at once with a run id.
/// </summary>
public interface IMarketAnalysisServiceClient
{
    /// <summary>Queues a run of the market analysis notebook.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The queued run, or a code: already running, rate limited, unauthorized, unavailable.</returns>
    Task<Result<MarketAnalysisRunStateDto>> StartRunAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads the state of a run.</summary>
    /// <param name="runId">The run id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The state, or the run not found code.</returns>
    Task<Result<MarketAnalysisRunStateDto>> GetRunAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>Reads the result of a completed run.</summary>
    /// <param name="runId">The run id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The result, or the run not found code.</returns>
    Task<Result<MarketAnalysisResultDto>> GetResultAsync(Guid runId, CancellationToken cancellationToken = default);
}
