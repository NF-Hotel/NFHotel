using System.Text.Json.Serialization;

using NFHotel.Application.Dto.Guests;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// A booking from the public page: the guest chooses a room type, not a room. The service picks a free room of that
/// type for the whole stay, so the room number is reception's to give and to change.
/// </summary>
/// <param name="CheckInDate">Arrival date, inclusive.</param>
/// <param name="CheckOutDate">Departure date, exclusive. Must be after the arrival date (BR-103).</param>
/// <param name="RoomSize">The room type the guest chose. Required: a missing type is refused by the binding, not read as the first one (MIL-086).</param>
/// <param name="Guests">How many people the room must sleep, 1 to 4; the service refuses any other number with the code of the field (MIL-086).</param>
/// <param name="NewGuest">The guest's own details. The public page never books for an existing guest (MIL-054).</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreatePublicBookingRequest(
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    [property: JsonRequired] RoomSize RoomSize,
    int Guests,
    GuestFields? NewGuest);
