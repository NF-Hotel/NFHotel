using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>Stores the records of started benchmark runs (ADR-0031).</summary>
public sealed class BenchmarkRequestRepository : Repository<BenchmarkRequest, int>, IBenchmarkRequestRepository
{
    /// <summary>Creates the repository.</summary>
    /// <param name="context">The database context.</param>
    public BenchmarkRequestRepository(HotelDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<BenchmarkRequest?> FindByRunIdAsync(Guid runId, CancellationToken cancellationToken = default) =>
        await Context.BenchmarkRequests
            .FirstOrDefaultAsync(request => request.RunId == runId, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<BenchmarkRequest>> ListNotDoneSinceAsync(
        DateTimeOffset since,
        CancellationToken cancellationToken = default) =>
        await Context.BenchmarkRequests
            .AsNoTracking()
            .Where(request => request.RequestedAt >= since && request.State != BenchmarkRequest.Done)
            .OrderByDescending(request => request.RequestedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
