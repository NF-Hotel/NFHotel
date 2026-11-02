using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;

namespace NFHotel.TestSupport;

/// <summary>An in-memory repository of the one row of pricing settings.</summary>
public sealed class InMemoryPricingSettingRepository : InMemoryRepository<PricingSetting>, IPricingSettingRepository
{
    public Task<PricingSetting?> GetAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault());
}
