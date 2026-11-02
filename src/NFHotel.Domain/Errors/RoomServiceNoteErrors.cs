namespace NFHotel.Domain.Errors;

/// <summary>
/// Error codes of the service notes of a room. A code is a stable string; the text for the user is the concern of the
/// presentation layer.
/// </summary>
public static class RoomServiceNoteErrors
{
    /// <summary>The note does not exist.</summary>
    public const string NotFound = "room_service_note.not_found";

    /// <summary>The room key of the note is not valid.</summary>
    public const string RoomInvalid = "room_service_note.room_invalid";

    /// <summary>The text of the note is missing.</summary>
    public const string TextRequired = "room_service_note.text_required";

    /// <summary>The text of the note is too long.</summary>
    public const string TextTooLong = "room_service_note.text_too_long";

    /// <summary>The name of the person who wrote the note is missing or too long.</summary>
    public const string WrittenByInvalid = "room_service_note.written_by_invalid";

    /// <summary>The name of the person who marked the note as done is missing or too long.</summary>
    public const string DoneByInvalid = "room_service_note.done_by_invalid";

    /// <summary>The note has gone to the history, so only a Manager can reopen it.</summary>
    public const string UndoWindowClosed = "room_service_note.undo_window_closed";
}
