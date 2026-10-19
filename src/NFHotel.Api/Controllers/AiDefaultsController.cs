using Microsoft.AspNetCore.Mvc;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.AiDefaults;
using NFHotel.Application.UseCases.AiDefaults;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The default AI model of the website and of the market analysis (ADR-0014). Every caller with the service token reads
/// the defaults (the market analysis service is one); only a Manager, with a staff token that holds
/// <see cref="Permissions.AiDefaultsManage"/>, sets one.
/// </summary>
[ApiController]
[Route(ApiRoutes.AiDefaults)]
public sealed class AiDefaultsController : ControllerBase
{
    private readonly IAiDefaultService _defaults;

    /// <summary>Creates the controller.</summary>
    /// <param name="defaults">Reads and sets the defaults.</param>
    public AiDefaultsController(IAiDefaultService defaults)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        _defaults = defaults;
    }

    /// <summary>Lists the defaults that are set.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The defaults.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _defaults.GetAllAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Sets the default of a purpose.</summary>
    /// <param name="purpose">The purpose: <c>website</c> or <c>market_analysis</c>.</param>
    /// <param name="request">The model and the run it was chosen from.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The default after the change, or the codes that say what is wrong.</returns>
    [HttpPut(ApiRoutes.AiDefaultByPurpose)]
    [RequiresPermission(Permissions.AiDefaultsManage)]
    public async Task<IActionResult> SetAsync(
        string purpose,
        [FromBody] SetAiDefaultRequest request,
        CancellationToken cancellationToken) =>
        this.ToResponse(
            await _defaults
                .SetAsync(purpose, request, User.FindFirst(JwtStaffTokenIssuer.NameClaim)?.Value ?? string.Empty, cancellationToken)
                .ConfigureAwait(false));
}
