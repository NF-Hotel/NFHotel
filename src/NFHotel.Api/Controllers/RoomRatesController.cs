using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.RoomRates;
using NFHotel.Application.UseCases.RoomRates;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The nightly base rate per room size (MIL-051). Staff who may see the rooms read the rates; only a Manager, with a staff
/// token that holds <see cref="Permissions.ManagePrices"/>, sets one. The writes are limited per account by the policy
/// <see cref="StaffController.WriteLimitPolicy"/>.
/// </summary>
[ApiController]
[Route(ApiRoutes.RoomRates)]
public sealed class RoomRatesController : ControllerBase
{
    private readonly IRoomRateService _rates;

    /// <summary>Creates the controller.</summary>
    /// <param name="rates">Reads and sets the rates.</param>
    public RoomRatesController(IRoomRateService rates)
    {
        ArgumentNullException.ThrowIfNull(rates);
        _rates = rates;
    }

    /// <summary>Lists the rates that are set.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The rates, ordered by room size.</returns>
    [HttpGet]
    [RequiresPermission(Permissions.RoomsView)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _rates.GetAllAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Sets the rate of a room size.</summary>
    /// <param name="size">The room size: <c>single</c>, <c>double</c> or <c>suite</c>, in any letter case.</param>
    /// <param name="request">The new nightly price.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The rate after the change, or the codes that say what is wrong.</returns>
    [HttpPut(ApiRoutes.RoomRateBySize)]
    [RequiresPermission(Permissions.ManagePrices)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> SetAsync(
        string size,
        [FromBody] SetRoomRateRequest request,
        CancellationToken cancellationToken) =>
        this.ToResponse(await _rates.SetAsync(size, request, cancellationToken).ConfigureAwait(false));
}
