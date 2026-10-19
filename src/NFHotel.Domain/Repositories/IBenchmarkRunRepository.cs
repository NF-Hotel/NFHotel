using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>Storage of the benchmark runs that the benchmark service delivers (ADR-0013).</summary>
public interface IBenchmarkRunRepository : IRepository<BenchmarkRun, int>
{
    /// <summary>Finds a run by the id the benchmark service gave it.</summary>
    /// <param name="runId">The run id of the contract.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The run, or <c>null</c> when it was never delivered.</returns>
    Task<BenchmarkRun?> FindByRunIdAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>Marks a run for removal; its requests are removed with it when the changes are saved.</summary>
    /// <param name="run">The run to remove.</param>
    void Remove(BenchmarkRun run);
}
