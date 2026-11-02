using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.Errors;

namespace NFHotel.Domain.Entities;

/// <summary>
/// A note on a room that says what service it needs, with who wrote it and who marked it as done.
/// </summary>
/// <remarks>
/// A note is open until somebody marks it as done. For <see cref="UndoWindow"/> after that, anybody can undo the mark.
/// Then it is in the history, with no job to move it there: the time of the mark alone decides. Only
/// <see cref="Reopen"/>, which the API gives to a Manager alone, takes a note back from the history.
/// <para>
/// Marking and undoing are idempotent, as on <see cref="Room"/> (A-08): a retry over an unstable network keeps the name
/// and the time of the first mark and gives no error.
/// </para>
/// </remarks>
public sealed class RoomServiceNote : Entity<int>
{
    /// <summary>The longest text of a note.</summary>
    public const int MaxTextLength = 1000;

    /// <summary>The longest user name that is stored.</summary>
    public const int MaxNameLength = 100;

    /// <summary>How long after the mark anybody can undo it.</summary>
    public static readonly TimeSpan UndoWindow = TimeSpan.FromMinutes(5);

    private RoomServiceNote()
    {
    }

    /// <summary>The room the note is about.</summary>
    [Range(1, int.MaxValue, ErrorMessage = RoomServiceNoteErrors.RoomInvalid)]
    public int RoomId { get; private set; }

    /// <summary>What the room needs.</summary>
    [Required(ErrorMessage = RoomServiceNoteErrors.TextRequired)]
    [StringLength(MaxTextLength, ErrorMessage = RoomServiceNoteErrors.TextTooLong)]
    public string Text { get; private set; } = string.Empty;

    /// <summary>The user name of the staff member who wrote the note.</summary>
    [Required(ErrorMessage = RoomServiceNoteErrors.WrittenByInvalid)]
    [StringLength(MaxNameLength, ErrorMessage = RoomServiceNoteErrors.WrittenByInvalid)]
    public string WrittenBy { get; private set; } = string.Empty;

    /// <summary>When the note was written.</summary>
    public DateTimeOffset WrittenAt { get; private set; }

    /// <summary>The user name of the staff member who marked the note as done, or <c>null</c> while it is open.</summary>
    public string? DoneBy { get; private set; }

    /// <summary>When the note was marked as done, or <c>null</c> while it is open.</summary>
    public DateTimeOffset? DoneAt { get; private set; }

    /// <summary>The moment the note goes to the history, or <c>null</c> while it is open.</summary>
    public DateTimeOffset? UndoableUntil => DoneAt + UndoWindow;

    /// <summary>Writes a new, open note.</summary>
    /// <param name="roomId">The room the note is about.</param>
    /// <param name="text">What the room needs.</param>
    /// <param name="writtenBy">Who writes it.</param>
    /// <param name="writtenAt">When it is written.</param>
    /// <returns>The new note.</returns>
    /// <exception cref="DomainException">A field breaks an invariant.</exception>
    public static RoomServiceNote Create(int roomId, string text, string writtenBy, DateTimeOffset writtenAt)
    {
        RoomServiceNote created = new()
        {
            RoomId = roomId,
            Text = text?.Trim() ?? string.Empty,
            WrittenBy = writtenBy?.Trim() ?? string.Empty,
            WrittenAt = writtenAt.ToUniversalTime(),
        };

        EntityValidation.ThrowIfInvalid(created);

        return created;
    }

    /// <summary>Tells whether the note is in the history: it was marked as done at least <see cref="UndoWindow"/> ago.</summary>
    /// <param name="now">The time now.</param>
    /// <returns>True if only a Manager can reopen the note.</returns>
    public bool IsInHistory(DateTimeOffset now) => now >= UndoableUntil;

    /// <summary>Marks the note as done. Is a no-op if it already is (A-08).</summary>
    /// <param name="doneBy">Who marks it.</param>
    /// <param name="doneAt">When it is marked.</param>
    /// <exception cref="DomainException">The name is missing or too long.</exception>
    public void MarkDone(string doneBy, DateTimeOffset doneAt)
    {
        if (DoneAt is not null)
        {
            return;
        }

        string name = doneBy?.Trim() ?? string.Empty;
        if (name.Length is 0 or > MaxNameLength)
        {
            throw new DomainException(RoomServiceNoteErrors.DoneByInvalid);
        }

        DoneBy = name;
        DoneAt = doneAt.ToUniversalTime();
    }

    /// <summary>Undoes the mark while the note is not yet in the history. Is a no-op if the note is open (A-08).</summary>
    /// <param name="now">The time now.</param>
    /// <exception cref="DomainException">The note is in the history.</exception>
    public void UndoDone(DateTimeOffset now)
    {
        if (IsInHistory(now))
        {
            throw new DomainException(RoomServiceNoteErrors.UndoWindowClosed);
        }

        Reopen();
    }

    /// <summary>Opens the note again, also from the history. Is a no-op if it is open (A-08).</summary>
    public void Reopen()
    {
        DoneBy = null;
        DoneAt = null;
    }
}
