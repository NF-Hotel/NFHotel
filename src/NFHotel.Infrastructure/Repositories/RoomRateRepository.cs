using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>Stores the nightly base rate per room size (MIL-055).</summary>
public sealed class RoomRateRepository : Repository<RoomRate, int>, IRoomRateRepository
{
    /// <summary>Creates the repository.</summary>
    /// <param name="context">The database context.</param>
    public RoomRateRepository(HotelDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<RoomRate?> FindBySizeAsync(RoomSize size, CancellationToken cancellationToken = default) =>
        await Context.RoomRates
            .FirstOrDefaultAsync(value => value.Size == size, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoomRate>> ListAsync(CancellationToken cancellationToken = default) =>
        await Context.RoomRates
            .AsNoTracking()
            .OrderBy(value => value.Size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
