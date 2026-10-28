using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;

namespace NFHotel.TestSupport;

/// <summary>An in-memory repository of the records of started benchmark runs (ADR-0031).</summary>
public sealed class InMemoryBenchmarkRequestRepository : InMemoryRepository<BenchmarkRequest>, IBenchmarkRequestRepository
{
    /// <inheritdoc />
    public Task<BenchmarkRequest?> FindByRunIdAsync(Guid runId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(request => request.RunId == runId));

    /// <inheritdoc />
    public Task<IReadOnlyList<BenchmarkRequest>> ListNotDoneSinceAsync(DateTimeOffset since, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BenchmarkRequest>>(
            [.. Items.Where(request => request.RequestedAt >= since && request.State != BenchmarkRequest.Done)
                .OrderByDescending(request => request.RequestedAt)]);
}
