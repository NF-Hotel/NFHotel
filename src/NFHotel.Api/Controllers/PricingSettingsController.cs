using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The pricing settings of the Manager (MIL-055). They are the Manager's rules, so both the read and the write need a staff
/// token that holds <see cref="Permissions.ManagePrices"/>. The writes are limited per account by the policy
/// <see cref="StaffController.WriteLimitPolicy"/>.
/// </summary>
[ApiController]
[Route(ApiRoutes.PricingSettings)]
public sealed class PricingSettingsController : ControllerBase
{
    private readonly IPricingSettingsService _settings;

    /// <summary>Creates the controller.</summary>
    /// <param name="settings">Reads and saves the settings.</param>
    public PricingSettingsController(IPricingSettingsService settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }

    /// <summary>Gets the settings.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The saved settings, or the defaults when none are saved yet.</returns>
    [HttpGet]
    [RequiresPermission(Permissions.ManagePrices)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _settings.GetAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Saves the settings.</summary>
    /// <param name="request">The new settings.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The settings after the change, or the codes that say what is wrong.</returns>
    [HttpPut]
    [RequiresPermission(Permissions.ManagePrices)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> SetAsync(
        [FromBody] SetPricingSettingsRequest request,
        CancellationToken cancellationToken) =>
        this.ToResponse(await _settings.SetAsync(request, cancellationToken).ConfigureAwait(false));
}
