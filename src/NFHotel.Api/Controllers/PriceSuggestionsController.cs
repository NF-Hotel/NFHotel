using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The open price suggestions of the Manager and their approval (MIL-051). Both need a staff token that holds
/// <see cref="Permissions.ManagePrices"/>. The approval is limited per account by the policy
/// <see cref="StaffController.WriteLimitPolicy"/>.
/// </summary>
[ApiController]
[Route(ApiRoutes.PriceSuggestions)]
public sealed class PriceSuggestionsController : ControllerBase
{
    private readonly IPriceSuggestionService _suggestions;

    /// <summary>Creates the controller.</summary>
    /// <param name="suggestions">Makes and approves the suggestions.</param>
    public PriceSuggestionsController(IPriceSuggestionService suggestions)
    {
        ArgumentNullException.ThrowIfNull(suggestions);
        _suggestions = suggestions;
    }

    /// <summary>Lists the suggestions that are open today.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The suggestions with their basis, and the AI explanation when the report has one.</returns>
    [HttpGet]
    [RequiresPermission(Permissions.ManagePrices)]
    public async Task<IActionResult> GetOpenAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _suggestions.GetOpenAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Approves a suggestion.</summary>
    /// <param name="request">The key of the suggestion and the optional dates.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The price change, or the codes that say what is wrong.</returns>
    [HttpPost(ApiRoutes.PriceSuggestionApprove)]
    [RequiresPermission(Permissions.ManagePrices)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> ApproveAsync(
        [FromBody] PriceSuggestionApproveRequest request,
        CancellationToken cancellationToken) =>
        this.ToResponse(await _suggestions.ApproveAsync(request, cancellationToken).ConfigureAwait(false));
}
