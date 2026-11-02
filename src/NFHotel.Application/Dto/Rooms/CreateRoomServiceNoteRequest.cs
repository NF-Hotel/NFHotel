namespace NFHotel.Application.Dto.Rooms;

/// <summary>A new service note. The name of the writer comes from the staff token, not from the request.</summary>
/// <param name="RoomId">The room the note is about.</param>
/// <param name="Text">What the room needs.</param>
public sealed record CreateRoomServiceNoteRequest(int RoomId, string Text);
