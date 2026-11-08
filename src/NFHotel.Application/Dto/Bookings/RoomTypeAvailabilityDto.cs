using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// The rooms of one size on one night in the availability calendar: how many are free, out of how many can be sold.
/// </summary>
/// <remarks>
/// Only rooms that can be let at all are counted (BR-110). A room under maintenance or out of service is neither
/// free nor part of the total, so the calendar never offers a room that the booking flow would refuse.
/// </remarks>
/// <param name="Size">The room size.</param>
/// <param name="Free">The number of rooms of the size that no booking blocks on the night.</param>
/// <param name="Sellable">The number of rooms of the size that can be let at all.</param>
/// <param name="FreeRooms">The room numbers of the free rooms, sorted.</param>
public sealed record RoomTypeAvailabilityDto(
    RoomSize Size,
    int Free,
    int Sellable,
    IReadOnlyList<string> FreeRooms);
