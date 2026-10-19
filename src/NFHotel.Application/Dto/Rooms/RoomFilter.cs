using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// Filter for the room search (BR-74, BR-76).
/// </summary>
/// <remarks>
/// All fields are optional: <c>null</c> means "do not filter on this". "Clear filter" is
/// therefore an empty <see cref="RoomFilter"/> — there is no separate use case for it.
/// The filter is an <i>object</i> and not loose parameters, because the number of criteria
/// was already on its way from three to five.
/// </remarks>
/// <param name="Floor">Floor, or <c>null</c> for all.</param>
/// <param name="Size">Room type, or <c>null</c> for all.</param>
/// <param name="Status">Bookability, or <c>null</c> for all.</param>
/// <param name="HousekeepingStatus">Cleaning and service state, or <c>null</c> for all.</param>
public sealed record RoomFilter(
    int? Floor = null,
    RoomSize? Size = null,
    RoomStatus? Status = null,
    HousekeepingStatus? HousekeepingStatus = null);
