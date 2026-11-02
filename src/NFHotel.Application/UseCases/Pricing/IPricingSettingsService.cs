using NFHotel.Application.Dto.Pricing;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>
/// Reads and saves the pricing settings of the Manager (MIL-055). The API implements it with the repository, and the web
/// application with a client over HTTP.
/// </summary>
public interface IPricingSettingsService
{
    /// <summary>Gets the settings.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The saved settings, or the defaults without a saver and a time when none are saved yet.</returns>
    Task<Result<PricingSettingsDto>> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the settings; earlier ones are replaced. The staff member who saves is the caller of the use case. One audit
    /// event lists the fields that changed, with the old and the new value; when nothing changed, nothing is saved.
    /// </summary>
    /// <param name="request">The new settings.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The settings after the call, or the codes that say what is wrong.</returns>
    Task<Result<PricingSettingsDto>> SetAsync(SetPricingSettingsRequest request, CancellationToken cancellationToken = default);
}
