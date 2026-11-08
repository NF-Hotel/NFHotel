namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// One night of the availability calendar: the free rooms of every room size.
/// </summary>
/// <remarks>
/// A night belongs to the day it starts on. A guest who leaves on a day does not block that night, because the
/// period of a booking is half-open (BR-39).
/// </remarks>
/// <param name="Night">The day the night starts on.</param>
/// <param name="Types">One entry for each room size, in the order of the sizes.</param>
public sealed record NightAvailabilityDto(
    DateOnly Night,
    IReadOnlyList<RoomTypeAvailabilityDto> Types);
