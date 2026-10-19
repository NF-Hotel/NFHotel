using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;

namespace NFHotel.TestSupport;

/// <summary>An in-memory repository of the benchmark runs that finds a run by the id the benchmark service gave it.</summary>
public sealed class InMemoryBenchmarkRunRepository : InMemoryRepository<BenchmarkRun>, IBenchmarkRunRepository
{
    public Task<BenchmarkRun?> FindByRunIdAsync(Guid runId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(run => run.RunId == runId));
}