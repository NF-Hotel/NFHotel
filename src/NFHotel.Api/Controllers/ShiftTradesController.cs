using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Shifts;
using NFHotel.Application.UseCases.Shifts;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The trades of shifts (MIL-060): a member gives a shift away or offers a swap, a colleague takes or accepts it, and a
/// Manager approves or rejects it.
/// </summary>
[ApiController]
[Route(ApiRoutes.ShiftTrades)]
public sealed class ShiftTradesController : ControllerBase
{
    private readonly IShiftService _shifts;

    /// <summary>Creates the controller.</summary>
    /// <param name="shifts">The use cases of the shifts and their trades.</param>
    public ShiftTradesController(IShiftService shifts)
    {
        ArgumentNullException.ThrowIfNull(shifts);
        _shifts = shifts;
    }

    /// <summary>Lists the trades the caller can act on or follow.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trades, newest first.</returns>
    [HttpGet]
    [RequiresPermission(Permissions.ShiftsView)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _shifts.ListTradesAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Gives an own shift away, or offers it in a swap.</summary>
    /// <param name="request">The shift, and the colleague's shift for a swap.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The open trade, 403, 404, 409, or 422 with the codes.</returns>
    [HttpPost]
    [RequiresPermission(Permissions.ShiftsTrade)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> OfferAsync([FromBody] OfferShiftTradeRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _shifts.OfferTradeAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Takes a shift that is given away.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade, 403, 404, 409, or 422 for another role.</returns>
    [HttpPost("{id:int}/take")]
    [RequiresPermission(Permissions.ShiftsTrade)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> TakeAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _shifts.TakeTradeAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Accepts a swap offered to the caller.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade, 403, 404, or 409.</returns>
    [HttpPost("{id:int}/accept")]
    [RequiresPermission(Permissions.ShiftsTrade)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> AcceptAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _shifts.AcceptTradeAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Declines a swap offered to the caller.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade, 403, 404, or 409.</returns>
    [HttpPost("{id:int}/decline")]
    [RequiresPermission(Permissions.ShiftsTrade)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> DeclineAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _shifts.DeclineTradeAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Takes back an own offer.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade, 403, 404, or 409.</returns>
    [HttpPost("{id:int}/withdraw")]
    [RequiresPermission(Permissions.ShiftsTrade)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> WithdrawAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _shifts.WithdrawTradeAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>A Manager approves a trade; the shifts change hands.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade, 404, or 409.</returns>
    [HttpPost("{id:int}/approve")]
    [RequiresPermission(Permissions.ShiftsManage)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> ApproveAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _shifts.ApproveTradeAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>A Manager rejects a trade.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade, 404, or 409.</returns>
    [HttpPost("{id:int}/reject")]
    [RequiresPermission(Permissions.ShiftsManage)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> RejectAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _shifts.RejectTradeAsync(id, cancellationToken).ConfigureAwait(false));
}
