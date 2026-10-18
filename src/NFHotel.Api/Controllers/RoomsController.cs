using Microsoft.AspNetCore.Mvc;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>The room use cases over HTTP: one endpoint per method of <see cref="IRoomService"/>.</summary>
[ApiController]
[Route(ApiRoutes.Rooms)]
public sealed class RoomsController : ControllerBase
{
    private readonly IRoomService _rooms;

    /// <summary>Creates the controller.</summary>
    /// <param name="rooms">The room use cases.</param>
    public RoomsController(IRoomService rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        _rooms = rooms;
    }

    /// <summary>Gets one room.</summary>
    /// <param name="id">The room key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The details, or 404.</returns>
    [HttpGet(ApiRoutes.ById)]
    [RequiresPermission(Permissions.RoomsView)]
    public async Task<IActionResult> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.GetByIdAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Lists every room.</summary>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>All rooms.</returns>
    [HttpGet]
    [RequiresPermission(Permissions.RoomsView)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.GetAllAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Lists the rooms that match a filter.</summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The matching rooms.</returns>
    [HttpPost(ApiRoutes.Search)]
    [RequiresPermission(Permissions.RoomsView)]
    public async Task<IActionResult> SearchAsync([FromBody] RoomFilter filter, CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.SearchAsync(filter, cancellationToken).ConfigureAwait(false));

    /// <summary>Gets the values of the room filter.</summary>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The floors and the enum values.</returns>
    [HttpGet(ApiRoutes.FilterOptions)]
    [RequiresPermission(Permissions.RoomsView)]
    public async Task<IActionResult> GetFilterOptionsAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.GetFilterOptionsAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Creates a room.</summary>
    /// <param name="request">The fields of the room.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The new room.</returns>
    [HttpPost]
    [RequiresPermission(Permissions.RoomsManage)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateRoomRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.CreateAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Updates a room.</summary>
    /// <param name="request">The key and the new fields.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The room after the change.</returns>
    [HttpPut]
    [RequiresPermission(Permissions.RoomsManage)]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateRoomRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.UpdateAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Deletes a room that has no bookings.</summary>
    /// <param name="id">The room key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200, or the reason it was refused.</returns>
    [HttpDelete(ApiRoutes.ById)]
    [RequiresPermission(Permissions.RoomsManage)]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.DeleteAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Takes a room out of service.</summary>
    /// <param name="id">The room key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The room after the change.</returns>
    [HttpPost(ApiRoutes.RoomAction.TakeOutOfService)]
    [RequiresPermission(Permissions.RoomsManage)]
    public async Task<IActionResult> TakeOutOfServiceAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.TakeOutOfServiceAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Sends a room to maintenance.</summary>
    /// <param name="id">The room key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The room after the change.</returns>
    [HttpPost(ApiRoutes.RoomAction.SendToMaintenance)]
    [RequiresPermission(Permissions.RoomsManage)]
    public async Task<IActionResult> SendToMaintenanceAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.SendToMaintenanceAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Returns a room to service.</summary>
    /// <param name="id">The room key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The room after the change.</returns>
    [HttpPost(ApiRoutes.RoomAction.ReturnToService)]
    [RequiresPermission(Permissions.RoomsManage)]
    public async Task<IActionResult> ReturnToServiceAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.ReturnToServiceAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Marks a room for daily cleaning.</summary>
    /// <param name="id">The room key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The room after the change.</returns>
    [HttpPost(ApiRoutes.RoomAction.MarkDailyCleaningDue)]
    [RequiresPermission(Permissions.UpdateHousekeepingStatus)]
    public async Task<IActionResult> MarkDailyCleaningDueAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.MarkDailyCleaningDueAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Marks a room for departure cleaning.</summary>
    /// <param name="id">The room key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The room after the change.</returns>
    [HttpPost(ApiRoutes.RoomAction.MarkDepartureCleaningDue)]
    [RequiresPermission(Permissions.UpdateHousekeepingStatus)]
    public async Task<IActionResult> MarkDepartureCleaningDueAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.MarkDepartureCleaningDueAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Starts cleaning a room.</summary>
    /// <param name="id">The room key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The room after the change.</returns>
    [HttpPost(ApiRoutes.RoomAction.StartCleaning)]
    [RequiresPermission(Permissions.UpdateHousekeepingStatus)]
    public async Task<IActionResult> StartCleaningAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.StartCleaningAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Completes the cleaning of a room.</summary>
    /// <param name="id">The room key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The room after the change.</returns>
    [HttpPost(ApiRoutes.RoomAction.CompleteCleaning)]
    [RequiresPermission(Permissions.UpdateHousekeepingStatus)]
    public async Task<IActionResult> CompleteCleaningAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.CompleteCleaningAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Reports that a room needs service.</summary>
    /// <param name="id">The room key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The room after the change.</returns>
    [HttpPost(ApiRoutes.RoomAction.ReportServiceNeeded)]
    [RequiresPermission(Permissions.UpdateHousekeepingStatus)]
    public async Task<IActionResult> ReportServiceNeededAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.ReportServiceNeededAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Starts service on a room.</summary>
    /// <param name="id">The room key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The room after the change.</returns>
    [HttpPost(ApiRoutes.RoomAction.StartService)]
    [RequiresPermission(Permissions.UpdateHousekeepingStatus)]
    public async Task<IActionResult> StartServiceAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.StartServiceAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Completes the service on a room.</summary>
    /// <param name="id">The room key.</param>
    /// <param name="requiresCleaning">Whether cleaning follows the service.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The room after the change.</returns>
    [HttpPost(ApiRoutes.RoomAction.CompleteService)]
    [RequiresPermission(Permissions.UpdateHousekeepingStatus)]
    public async Task<IActionResult> CompleteServiceAsync(int id, [FromQuery] bool requiresCleaning, CancellationToken cancellationToken) =>
        this.ToResponse(await _rooms.CompleteServiceAsync(id, requiresCleaning, cancellationToken).ConfigureAwait(false));
}