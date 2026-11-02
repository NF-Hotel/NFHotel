using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The service notes of the rooms. Staff who may change the housekeeping status write a note; everybody who may see the
/// rooms marks one as done and undoes the mark while it is not in the history; only a Manager, with a staff token that
/// holds <see cref="Permissions.RoomsManage"/>, takes one back from the history. The writes are limited per account by
/// the policy <see cref="StaffController.WriteLimitPolicy"/>.
/// </summary>
[ApiController]
[Route(ApiRoutes.RoomServiceNotes)]
public sealed class RoomServiceNotesController : ControllerBase
{
    private readonly IRoomServiceNoteService _notes;

    /// <summary>Creates the controller.</summary>
    /// <param name="notes">The use cases of the notes.</param>
    public RoomServiceNotesController(IRoomServiceNoteService notes)
    {
        ArgumentNullException.ThrowIfNull(notes);
        _notes = notes;
    }

    /// <summary>Lists the notes of every room that are not in the history.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The notes, oldest first.</returns>
    [HttpGet]
    [RequiresPermission(Permissions.RoomsView)]
    public async Task<IActionResult> GetCurrentAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _notes.GetCurrentAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Lists the finished service of a room.</summary>
    /// <param name="roomId">The room key.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The notes in the history, most recently done first, or 404.</returns>
    [HttpGet(ApiRoutes.RoomServiceNoteHistory)]
    [RequiresPermission(Permissions.RoomsView)]
    public async Task<IActionResult> GetHistoryAsync(int roomId, CancellationToken cancellationToken) =>
        this.ToResponse(await _notes.GetHistoryAsync(roomId, cancellationToken).ConfigureAwait(false));

    /// <summary>Writes a note and reports that the room needs service. The name of the writer comes from the staff token.</summary>
    /// <param name="request">The room and the text.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The new note, or the codes that say what is wrong.</returns>
    [HttpPost]
    [RequiresPermission(Permissions.UpdateHousekeepingStatus)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateRoomServiceNoteRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _notes.CreateAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Marks a note as done.</summary>
    /// <param name="id">The note key.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The note after the change, or 404.</returns>
    [HttpPost(ApiRoutes.RoomServiceNoteDone)]
    [RequiresPermission(Permissions.RoomsView)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> MarkDoneAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _notes.MarkDoneAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Undoes the mark of a note that is not yet in the history.</summary>
    /// <param name="id">The note key.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The note after the change, 404, or 409 when it has gone to the history.</returns>
    [HttpPost(ApiRoutes.RoomServiceNoteUndoDone)]
    [RequiresPermission(Permissions.RoomsView)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> UndoDoneAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _notes.UndoDoneAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Takes a note back from the history. For a Manager only.</summary>
    /// <param name="id">The note key.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The note after the change, or 404.</returns>
    [HttpPost(ApiRoutes.RoomServiceNoteReopen)]
    [RequiresPermission(Permissions.RoomsManage)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> ReopenAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _notes.ReopenAsync(id, cancellationToken).ConfigureAwait(false));
}
