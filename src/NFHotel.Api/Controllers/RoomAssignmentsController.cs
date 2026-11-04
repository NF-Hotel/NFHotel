using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The housekeeper of each room (MIL-060): housekeepers and Managers read them, a Manager assigns them. The trades of
/// rooms are in <see cref="RoomTradesController"/>.
/// </summary>
[ApiController]
[Route(ApiRoutes.RoomAssignments)]
public sealed class RoomAssignmentsController : ControllerBase
{
    private readonly IRoomAssignmentService _assignments;

    /// <summary>Creates the controller.</summary>
    /// <param name="assignments">The use cases of the assignments.</param>
    public RoomAssignmentsController(IRoomAssignmentService assignments)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        _assignments = assignments;
    }

    /// <summary>Reads every room with its housekeeper, and every active housekeeper with the number of rooms.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The overview.</returns>
    [HttpGet]
    [RequiresPermission(Permissions.RoomAssignmentsView)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _assignments.GetOverviewAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Gives a room to a housekeeper, or leaves it without one.</summary>
    /// <param name="roomId">The room.</param>
    /// <param name="request">The housekeeper, or none.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>200, 404, 409, or 422 for somebody who is not an active housekeeper.</returns>
    [HttpPut(ApiRoutes.ByRoomId)]
    [RequiresPermission(Permissions.RoomAssignmentsManage)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> AssignAsync(int roomId, [FromBody] AssignRoomRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _assignments.AssignAsync(roomId, request, cancellationToken).ConfigureAwait(false));
}
