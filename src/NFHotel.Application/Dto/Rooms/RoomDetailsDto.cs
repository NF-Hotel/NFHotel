using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// A room with everything the detail screen needs.
/// </summary>
/// <param name="RoomId">The room id.</param>
/// <param name="RoomNumber">Room number.</param>
/// <param name="Floor">Etage.</param>
/// <param name="Size">Room type.</param>
/// <param name="Capacity">The number of people the room can hold.</param>
/// <param name="Status">Bookbarhed (B-04).</param>
/// <param name="HousekeepingStatus">Cleaning and service state (B-04).</param>
/// <param name="IsBookable">
/// Whether the room may be let at all (BR-110). Computed by the domain's
/// <see cref="Room.IsBookable"/> — Web must not derive it from <paramref name="Status"/> itself.
/// </param>
/// <param name="HasBookings">
/// Whether there are bookings for the room. Carries the delete guard (BR-78, BR-112) up into the UI, so
/// the warning can be shown before the button is pressed instead of as an error afterwards.
/// </param>
public sealed record RoomDetailsDto(
    int RoomId,
    string RoomNumber,
    int Floor,
    RoomSize Size,
    int Capacity,
    RoomStatus Status,
    HousekeepingStatus HousekeepingStatus,
    bool IsBookable,
    bool HasBookings)
{
    /// <summary>
    /// Which transitions are legal right now (A-07), so Web can draw buttons without knowing
    /// a single transition.
    /// </summary>
    /// <remarks>
    /// A computed property and not a constructor parameter, because the detail view is fetched as a
    /// SQL projection in the repository (A-11): the database cannot fill a
    /// <see cref="RoomActionsDto"/> in the row. The expressions therefore live in one place in Application —
    /// <see cref="RoomMapping.ToActions(RoomStatus, HousekeepingStatus)"/> — which both this
    /// property and the entity path use.
    /// </remarks>
    public RoomActionsDto Actions => RoomMapping.ToActions(Status, HousekeepingStatus);
}
