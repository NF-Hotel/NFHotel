using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>Storage of the market analyses that the data analysis service delivers (MIL-026).</summary>
public interface IMarketAnalysisRunRepository : IRepository<MarketAnalysisRun, int>
{
    /// <summary>Finds a run by the id the data analysis service gave it.</summary>
    /// <param name="runId">The run id of the service.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The run, or <c>null</c> when it was never stored.</returns>
    Task<MarketAnalysisRun?> FindByRunIdAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>Finds the run that finished last.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The run, or <c>null</c> when none is stored.</returns>
    Task<MarketAnalysisRun?> FindLatestAsync(CancellationToken cancellationToken = default);
}
