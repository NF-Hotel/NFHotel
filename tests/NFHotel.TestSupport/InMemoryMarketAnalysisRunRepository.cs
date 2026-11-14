using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;

namespace NFHotel.TestSupport;

/// <summary>An in-memory repository of the stored market analyses.</summary>
public sealed class InMemoryMarketAnalysisRunRepository : InMemoryRepository<MarketAnalysisRun>, IMarketAnalysisRunRepository
{
    public Task<MarketAnalysisRun?> FindByRunIdAsync(Guid runId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(run => run.RunId == runId));

    public Task<MarketAnalysisRun?> FindLatestAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.OrderByDescending(run => run.FinishedAt).ThenByDescending(run => run.Id).FirstOrDefault());
}
