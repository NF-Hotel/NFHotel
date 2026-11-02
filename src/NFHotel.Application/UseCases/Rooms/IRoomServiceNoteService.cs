using NFHotel.Application.Dto.Rooms;

namespace NFHotel.Application.UseCases.Rooms;

/// <summary>The use cases of the service notes of the rooms.</summary>
public interface IRoomServiceNoteService
{
    /// <summary>Lists the notes of every room that are not in the history.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The open notes and the ones whose mark can still be undone, oldest first.</returns>
    Task<Result<IReadOnlyList<RoomServiceNoteDto>>> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists the finished service of a room.</summary>
    /// <param name="roomId">The room.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The notes in the history, most recently done first, or the code of a room that does not exist.</returns>
    Task<Result<IReadOnlyList<RoomServiceNoteDto>>> GetHistoryAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>Writes a note and reports that the room needs service.</summary>
    /// <param name="request">The room and the text.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The new note, or the codes that say what is wrong.</returns>
    /// <remarks>A room that is already under service stays under service; the note is added to the work.</remarks>
    Task<Result<RoomServiceNoteDto>> CreateAsync(CreateRoomServiceNoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Marks a note as done. Does not change the state of the room: the service flow of the room says that.</summary>
    /// <param name="noteId">The note.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The note after the change.</returns>
    Task<Result<RoomServiceNoteDto>> MarkDoneAsync(int noteId, CancellationToken cancellationToken = default);

    /// <summary>Undoes the mark of a note that is not yet in the history.</summary>
    /// <param name="noteId">The note.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The note after the change, or the code that says it has gone to the history.</returns>
    Task<Result<RoomServiceNoteDto>> UndoDoneAsync(int noteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes a note back from the history, and reports that the room needs service again. The API lets only a Manager
    /// call it. The audit trail keeps who had marked the note as done and when.
    /// </summary>
    /// <param name="noteId">The note.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The note after the change.</returns>
    Task<Result<RoomServiceNoteDto>> ReopenAsync(int noteId, CancellationToken cancellationToken = default);
}
