using System.Globalization;

using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Rooms;

/// <summary>Writes, marks and lists the service notes of the rooms. The rules of the marks are on <see cref="RoomServiceNote"/>.</summary>
public sealed class RoomServiceNoteService : IRoomServiceNoteService
{
    private readonly IRoomServiceNoteRepository _notes;
    private readonly IRoomRepository _rooms;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IStaffCallContext _context;
    private readonly StaffAuditRecorder _recorder;

    /// <summary>Creates the service.</summary>
    /// <param name="notes">The storage of the notes.</param>
    /// <param name="rooms">The storage of the rooms.</param>
    /// <param name="audit">The audit trail a reopened note is written to.</param>
    /// <param name="unitOfWork">Saves a note, its room and its audit event in one transaction.</param>
    /// <param name="clock">Says when a note was written or marked, and which notes are in the history.</param>
    /// <param name="context">Who is calling.</param>
    public RoomServiceNoteService(
        IRoomServiceNoteRepository notes,
        IRoomRepository rooms,
        IStaffAuditRepository audit,
        IUnitOfWork unitOfWork,
        IClock clock,
        IStaffCallContext context)
    {
        ArgumentNullException.ThrowIfNull(notes);
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(context);

        _notes = notes;
        _rooms = rooms;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _context = context;
        _recorder = new StaffAuditRecorder(audit, context, clock);
    }

    private DateTimeOffset HistoryCutoff => _clock.UtcNow - RoomServiceNote.UndoWindow;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RoomServiceNoteDto>>> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RoomServiceNote> notes = await _notes.ListCurrentAsync(HistoryCutoff, cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<RoomServiceNoteDto>>.Success(notes.Select(ToDto).ToArray());
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RoomServiceNoteDto>>> GetHistoryAsync(int roomId, CancellationToken cancellationToken = default)
    {
        Room? room = await _rooms.GetByIdAsync(roomId, cancellationToken).ConfigureAwait(false);
        if (room is null)
        {
            return Result<IReadOnlyList<RoomServiceNoteDto>>.Failure(ErrorCodes.Room.NotFound);
        }

        IReadOnlyList<RoomServiceNote> notes = await _notes.ListHistoryAsync(roomId, HistoryCutoff, cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<RoomServiceNoteDto>>.Success(notes.Select(ToDto).ToArray());
    }

    /// <inheritdoc />
    public async Task<Result<RoomServiceNoteDto>> CreateAsync(CreateRoomServiceNoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Room? room = await _rooms.GetByIdAsync(request.RoomId, cancellationToken).ConfigureAwait(false);
        if (room is null)
        {
            return Result<RoomServiceNoteDto>.Failure(ErrorCodes.Room.NotFound);
        }

        RoomServiceNote note;
        try
        {
            note = RoomServiceNote.Create(request.RoomId, request.Text, _context.StaffName ?? string.Empty, _clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return Result<RoomServiceNoteDto>.FromCodes(exception.Codes);
        }

        _notes.Add(note);
        ReportServiceNeeded(room);

        return await SaveAsync(note, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Result<RoomServiceNoteDto>> MarkDoneAsync(int noteId, CancellationToken cancellationToken = default) =>
        ChangeAsync(noteId, note => note.MarkDone(_context.StaffName ?? string.Empty, _clock.UtcNow), cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomServiceNoteDto>> UndoDoneAsync(int noteId, CancellationToken cancellationToken = default) =>
        ChangeAsync(noteId, note => note.UndoDone(_clock.UtcNow), cancellationToken);

    /// <inheritdoc />
    public async Task<Result<RoomServiceNoteDto>> ReopenAsync(int noteId, CancellationToken cancellationToken = default)
    {
        RoomServiceNote? note = await _notes.GetByIdAsync(noteId, cancellationToken).ConfigureAwait(false);
        if (note is null)
        {
            return Result<RoomServiceNoteDto>.Failure(RoomServiceNoteErrors.NotFound);
        }

        // A retry finds the note open already: it changes nothing and must not leave a second audit event.
        if (note.DoneAt is { } doneAt)
        {
            _recorder.ByCallerWithDetail(
                StaffAuditAction.RoomServiceNoteReopened,
                StaffAuditRecorder.Success,
                string.Create(CultureInfo.InvariantCulture, $"#{note.Id} room {note.RoomId}: done by {note.DoneBy} at {doneAt:yyyy-MM-dd HH:mm} UTC"));
            note.Reopen();
            _notes.Update(note);

            Room? room = await _rooms.GetByIdAsync(note.RoomId, cancellationToken).ConfigureAwait(false);
            if (room is not null)
            {
                ReportServiceNeeded(room);
            }
        }

        return await SaveAsync(note, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<RoomServiceNoteDto>> ChangeAsync(int noteId, Action<RoomServiceNote> change, CancellationToken cancellationToken)
    {
        RoomServiceNote? note = await _notes.GetByIdAsync(noteId, cancellationToken).ConfigureAwait(false);
        if (note is null)
        {
            return Result<RoomServiceNoteDto>.Failure(RoomServiceNoteErrors.NotFound);
        }

        try
        {
            change(note);
        }
        catch (DomainException exception)
        {
            return Result<RoomServiceNoteDto>.FromCodes(exception.Codes);
        }

        _notes.Update(note);

        return await SaveAsync(note, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Flags the room. A room already under service has nothing to report, and the domain would refuse the call.</summary>
    private void ReportServiceNeeded(Room room)
    {
        if (room.CanReportServiceNeeded)
        {
            room.ReportServiceNeeded();
            _rooms.Update(room);
        }
    }

    private async Task<Result<RoomServiceNoteDto>> SaveAsync(RoomServiceNote note, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException)
        {
            // The xmin of the room: another member of staff moved the room at the same moment.
            return Result<RoomServiceNoteDto>.Failure(ErrorCodes.Room.ConcurrencyConflict);
        }

        return Result<RoomServiceNoteDto>.Success(ToDto(note));
    }

    private static RoomServiceNoteDto ToDto(RoomServiceNote note) =>
        new(note.Id, note.RoomId, note.Text, note.WrittenBy, note.WrittenAt, note.DoneBy, note.DoneAt, note.UndoableUntil);
}
