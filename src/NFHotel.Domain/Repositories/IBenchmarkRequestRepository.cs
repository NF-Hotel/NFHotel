using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>Storage of the records of started benchmark runs (ADR-0031).</summary>
public interface IBenchmarkRequestRepository : IRepository<BenchmarkRequest, int>
{
    /// <summary>Finds the record of a run by the id the benchmark service gave it.</summary>
    /// <param name="runId">The run id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The record, or <c>null</c> when the run was never recorded.</returns>
    Task<BenchmarkRequest?> FindByRunIdAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>Lists the records started at or after a moment that are not done, newest first.</summary>
    /// <param name="since">The earliest start to include.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The records.</returns>
    Task<IReadOnlyList<BenchmarkRequest>> ListNotDoneSinceAsync(DateTimeOffset since, CancellationToken cancellationToken = default);
}
