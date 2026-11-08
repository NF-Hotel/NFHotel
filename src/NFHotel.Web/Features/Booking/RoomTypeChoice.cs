using NFHotel.Domain.ValueObjects;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// One room type the guest can choose on <c>/book</c>: how many rooms of it are free for the whole stay and sleep the
/// party, and how many people the biggest of them sleeps. The guest never sees a room number; the API picks the room.
/// </summary>
/// <param name="Size">The room type.</param>
/// <param name="Free">The free rooms of the type that sleep the party.</param>
/// <param name="Sleeps">How many people the biggest free room of the type sleeps.</param>
public sealed record RoomTypeChoice(RoomSize Size, int Free, int Sleeps);
