using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>Stores the service notes of the rooms.</summary>
public sealed class RoomServiceNoteRepository : Repository<RoomServiceNote, int>, IRoomServiceNoteRepository
{
    /// <summary>Creates the repository.</summary>
    /// <param name="context">The database context.</param>
    public RoomServiceNoteRepository(HotelDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoomServiceNote>> ListCurrentAsync(DateTimeOffset historyCutoff, CancellationToken cancellationToken = default) =>
        await Context.RoomServiceNotes
            .AsNoTracking()
            .Where(note => note.DoneAt == null || note.DoneAt > historyCutoff)
            .OrderBy(note => note.WrittenAt)
            .ThenBy(note => note.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoomServiceNote>> ListHistoryAsync(int roomId, DateTimeOffset historyCutoff, CancellationToken cancellationToken = default) =>
        await Context.RoomServiceNotes
            .AsNoTracking()
            .Where(note => note.RoomId == roomId && note.DoneAt <= historyCutoff)
            .OrderByDescending(note => note.DoneAt)
            .ThenByDescending(note => note.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
