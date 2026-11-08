using NFHotel.Application.Dto.Guests;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// A booking from the public page: the guest chooses a room type, not a room. The service picks a free room of that
/// type for the whole stay, so the room number is reception's to give and to change.
/// </summary>
/// <param name="CheckInDate">Arrival date, inclusive.</param>
/// <param name="CheckOutDate">Departure date, exclusive. Must be after the arrival date (BR-103).</param>
/// <param name="RoomSize">The room type the guest chose.</param>
/// <param name="Guests">How many people the room must sleep. Less than one counts as one.</param>
/// <param name="NewGuest">The guest's own details. The public page never books for an existing guest (MIL-054).</param>
public sealed record CreatePublicBookingRequest(
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    RoomSize RoomSize,
    int Guests,
    GuestFields? NewGuest);
