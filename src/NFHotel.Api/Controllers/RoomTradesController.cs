using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The trades of rooms between housekeepers (MIL-060): a housekeeper offers, the colleague accepts or declines, and a
/// Manager approves or rejects a trade that leaves somebody below the minimum of rooms.
/// </summary>
[ApiController]
[Route(ApiRoutes.RoomTrades)]
public sealed class RoomTradesController : ControllerBase
{
    private readonly IRoomAssignmentService _assignments;

    /// <summary>Creates the controller.</summary>
    /// <param name="assignments">The use cases of the assignments and their trades.</param>
    public RoomTradesController(IRoomAssignmentService assignments)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        _assignments = assignments;
    }

    /// <summary>Lists the trades of the caller, and for a Manager the trades that wait for approval.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trades, newest first.</returns>
    [HttpGet]
    [RequiresPermission(Permissions.RoomAssignmentsView)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _assignments.ListTradesAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Gives rooms to a colleague, or trades them for rooms of the colleague.</summary>
    /// <param name="request">The colleague and the rooms.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The open trade, or 422 with the codes.</returns>
    [HttpPost]
    [RequiresPermission(Permissions.RoomAssignmentsTrade)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> OfferAsync([FromBody] OfferRoomTradeRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _assignments.OfferTradeAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>The colleague accepts the trade.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade, 403 for somebody else's trade, 404, or 409.</returns>
    [HttpPost("{id:int}/accept")]
    [RequiresPermission(Permissions.RoomAssignmentsTrade)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> AcceptAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _assignments.AcceptTradeAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>The colleague declines the trade.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade, 403, 404, or 409.</returns>
    [HttpPost("{id:int}/decline")]
    [RequiresPermission(Permissions.RoomAssignmentsTrade)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> DeclineAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _assignments.DeclineTradeAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>The housekeeper who offered the trade takes it back.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade, 403, 404, or 409.</returns>
    [HttpPost("{id:int}/withdraw")]
    [RequiresPermission(Permissions.RoomAssignmentsTrade)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> WithdrawAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _assignments.WithdrawTradeAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>A Manager approves a trade that waits for approval.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade, 404, or 409.</returns>
    [HttpPost("{id:int}/approve")]
    [RequiresPermission(Permissions.RoomAssignmentsManage)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> ApproveAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _assignments.ApproveTradeAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>A Manager rejects a trade that waits for approval.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade, 404, or 409.</returns>
    [HttpPost("{id:int}/reject")]
    [RequiresPermission(Permissions.RoomAssignmentsManage)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> RejectAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _assignments.RejectTradeAsync(id, cancellationToken).ConfigureAwait(false));
}
