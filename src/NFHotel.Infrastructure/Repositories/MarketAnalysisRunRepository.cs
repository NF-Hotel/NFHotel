using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>Stores the market analyses that the data analysis service delivers (MIL-026).</summary>
public sealed class MarketAnalysisRunRepository : Repository<MarketAnalysisRun, int>, IMarketAnalysisRunRepository
{
    /// <summary>Creates the repository.</summary>
    /// <param name="context">The database context.</param>
    public MarketAnalysisRunRepository(HotelDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<MarketAnalysisRun?> FindByRunIdAsync(Guid runId, CancellationToken cancellationToken = default) =>
        await Context.MarketAnalysisRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(run => run.RunId == runId, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<MarketAnalysisRun?> FindLatestAsync(CancellationToken cancellationToken = default) =>
        await Context.MarketAnalysisRuns
            .AsNoTracking()
            .OrderByDescending(run => run.FinishedAt)
            .ThenByDescending(run => run.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
}
