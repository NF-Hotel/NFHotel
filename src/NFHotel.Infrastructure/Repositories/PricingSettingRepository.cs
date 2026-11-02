using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>Stores the one row of pricing settings (MIL-055).</summary>
public sealed class PricingSettingRepository : Repository<PricingSetting, int>, IPricingSettingRepository
{
    /// <summary>Creates the repository.</summary>
    /// <param name="context">The database context.</param>
    public PricingSettingRepository(HotelDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<PricingSetting?> GetAsync(CancellationToken cancellationToken = default) =>
        await Context.PricingSettings
            .FirstOrDefaultAsync(value => value.Slot == PricingSetting.SingletonSlot, cancellationToken)
            .ConfigureAwait(false);
}
