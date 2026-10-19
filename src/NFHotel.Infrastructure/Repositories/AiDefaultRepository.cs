using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>Stores the default AI model per purpose (ADR-0014).</summary>
public sealed class AiDefaultRepository : Repository<AiDefault, int>, IAiDefaultRepository
{
    /// <summary>Creates the repository.</summary>
    /// <param name="context">The database context.</param>
    public AiDefaultRepository(HotelDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<AiDefault?> FindByPurposeAsync(string purpose, CancellationToken cancellationToken = default) =>
        await Context.AiDefaults
            .FirstOrDefaultAsync(value => value.Purpose == purpose, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiDefault>> ListAsync(CancellationToken cancellationToken = default) =>
        await Context.AiDefaults
            .AsNoTracking()
            .OrderBy(value => value.Purpose)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
