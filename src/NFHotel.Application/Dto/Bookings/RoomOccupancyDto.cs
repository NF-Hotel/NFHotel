using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// One booking's occupation of a room, for the availability lookup.
/// </summary>
/// <remarks>
/// Deliberately as narrow as possible: only what <see cref="NFHotel.Domain.Entities.Booking"/> needs to decide
/// whether the room is occupied. The alternative — fetching all bookings in the period as entities and
/// filtering in memory — is exactly the fault BR-111 describes.
/// <para>
/// Note that the overlap decision is <b>not</b> made here and not in SQL either: the service
/// builds a <c>DateRange</c> from the fields and asks the domain (A-01, A-04). Otherwise the
/// predicate would exist in two places and could disagree about <c>&lt;</c> versus <c>&lt;=</c>.
/// </para>
/// </remarks>
/// <param name="BookingId">The id of the booking, so a booking can be left out when editing.</param>
/// <param name="RoomId">The room that is occupied.</param>
/// <param name="StartDate">The first occupied night.</param>
/// <param name="EffectiveEndDate">The first free date after the occupation (A-01).</param>
/// <param name="Status">The status of the booking, so <c>BookingStatusExtensions.BlocksRoom</c> can be checked.</param>
public sealed record RoomOccupancyDto(
    int BookingId,
    int RoomId,
    DateOnly StartDate,
    DateOnly EffectiveEndDate,
    BookingStatus Status);