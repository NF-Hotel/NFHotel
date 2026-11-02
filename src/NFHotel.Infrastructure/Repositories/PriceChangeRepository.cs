using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>Stores the price changes of the room sizes (MIL-055).</summary>
public sealed class PriceChangeRepository : Repository<PriceChange, int>, IPriceChangeRepository
{
    /// <summary>Creates the repository.</summary>
    /// <param name="context">The database context.</param>
    public PriceChangeRepository(HotelDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PriceChange>> ListForSizeAsync(
        RoomSize size,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default) =>
        await Context.PriceChanges
            .AsNoTracking()
            .Where(value => value.Size == size && value.FirstDay <= to && value.LastDay >= from)
            .OrderBy(value => value.FirstDay)
            .ThenBy(value => value.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PriceChange>> ListAsync(CancellationToken cancellationToken = default) =>
        await Context.PriceChanges
            .AsNoTracking()
            .OrderBy(value => value.FirstDay)
            .ThenBy(value => value.Size)
            .ThenBy(value => value.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
