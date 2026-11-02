using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>Storage of the service notes of the rooms.</summary>
public interface IRoomServiceNoteRepository : IRepository<RoomServiceNote, int>
{
    /// <summary>Lists the notes of every room that are not in the history, oldest first.</summary>
    /// <param name="historyCutoff">A note marked as done at or before this moment is in the history.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The open notes and the ones whose mark can still be undone.</returns>
    Task<IReadOnlyList<RoomServiceNote>> ListCurrentAsync(DateTimeOffset historyCutoff, CancellationToken cancellationToken = default);

    /// <summary>Lists the notes of one room that are in the history, most recently done first.</summary>
    /// <param name="roomId">The room.</param>
    /// <param name="historyCutoff">A note marked as done at or before this moment is in the history.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The notes in the history.</returns>
    Task<IReadOnlyList<RoomServiceNote>> ListHistoryAsync(int roomId, DateTimeOffset historyCutoff, CancellationToken cancellationToken = default);
}
