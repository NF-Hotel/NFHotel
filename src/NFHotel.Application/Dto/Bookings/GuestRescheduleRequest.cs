using System.Text.Json.Serialization;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// A signed-in guest's move of the own booking to new dates (MIL-018, ADR-0049 decision 3). It names no booking and no
/// room: the booking is the one in the guest token, and the guest keeps the room.
/// </summary>
/// <param name="CheckInDate">The new arrival date.</param>
/// <param name="CheckOutDate">The new departure date, exclusive.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record GuestRescheduleRequest(DateOnly CheckInDate, DateOnly CheckOutDate);
