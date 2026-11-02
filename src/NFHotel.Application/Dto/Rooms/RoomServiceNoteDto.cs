namespace NFHotel.Application.Dto.Rooms;

/// <summary>A service note of a room.</summary>
/// <param name="Id">The key of the note.</param>
/// <param name="RoomId">The room the note is about.</param>
/// <param name="Text">What the room needs.</param>
/// <param name="WrittenBy">The user name of the staff member who wrote it.</param>
/// <param name="WrittenAt">When it was written.</param>
/// <param name="DoneBy">The user name of the staff member who marked it as done, or <c>null</c> while it is open.</param>
/// <param name="DoneAt">When it was marked as done, or <c>null</c> while it is open.</param>
/// <param name="UndoableUntil">The moment it goes to the history, or <c>null</c> while it is open.</param>
public sealed record RoomServiceNoteDto(
    int Id,
    int RoomId,
    string Text,
    string WrittenBy,
    DateTimeOffset WrittenAt,
    string? DoneBy,
    DateTimeOffset? DoneAt,
    DateTimeOffset? UndoableUntil)
{
    /// <summary>True when the note is marked as done.</summary>
    public bool IsDone => DoneAt is not null;
}
