using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>Storage of the one row of pricing settings (MIL-055).</summary>
public interface IPricingSettingRepository : IRepository<PricingSetting, int>
{
    /// <summary>Gets the stored settings.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The settings, or <c>null</c> when none are stored yet.</returns>
    Task<PricingSetting?> GetAsync(CancellationToken cancellationToken = default);
}
