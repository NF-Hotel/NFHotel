using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The price changes of the Manager (MIL-055): the list, the revert and the effective prices of a day. Every action needs a
/// staff token that holds <see cref="Permissions.ManagePrices"/>. The revert is limited per account by the policy
/// <see cref="StaffController.WriteLimitPolicy"/>.
/// </summary>
[ApiController]
[Route(ApiRoutes.PriceChanges)]
public sealed class PriceChangesController : ControllerBase
{
    private readonly IPriceSuggestionService _suggestions;

    /// <summary>Creates the controller.</summary>
    /// <param name="suggestions">Lists and reverts the changes.</param>
    public PriceChangesController(IPriceSuggestionService suggestions)
    {
        ArgumentNullException.ThrowIfNull(suggestions);
        _suggestions = suggestions;
    }

    /// <summary>Lists every price change.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The changes, the latest period first.</returns>
    [HttpGet]
    [RequiresPermission(Permissions.ManagePrices)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _suggestions.GetChangesAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Gives the effective price of every room size on a day.</summary>
    /// <param name="day">The day as <c>yyyy-MM-dd</c>; today when left out.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>One price per room size.</returns>
    [HttpGet(ApiRoutes.PriceChangesEffective)]
    [RequiresPermission(Permissions.ManagePrices)]
    public async Task<IActionResult> GetEffectiveAsync([FromQuery] DateOnly? day, CancellationToken cancellationToken) =>
        this.ToResponse(await _suggestions.GetEffectivePricesAsync(day, cancellationToken).ConfigureAwait(false));

    /// <summary>Reverts a price change.</summary>
    /// <param name="id">The key of the change.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The reverted change, or the codes that say what is wrong.</returns>
    [HttpPost(ApiRoutes.PriceChangeRevert)]
    [RequiresPermission(Permissions.ManagePrices)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> RevertAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _suggestions.RevertAsync(id, cancellationToken).ConfigureAwait(false));
}
