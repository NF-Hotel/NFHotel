namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// Room type. Replaces the old free-text field <c>Room.RoomSize</c>.
/// </summary>
/// <remarks>
/// The ordinal values are new — there is no existing int data to keep, only text
/// (<c>'Single'</c>/<c>'Double'</c>/<c>'Suite'</c>) that the infrastructure layer must map
/// at migration. Later becomes the hook for a <c>RoomType</c> with a price.
/// </remarks>
public enum RoomSize
{
    /// <summary>Single room.</summary>
    Single = 0,

    /// <summary>Double room.</summary>
    Double = 1,

    /// <summary>Suite.</summary>
    Suite = 2
}