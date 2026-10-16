using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// Repositoryets projektion af en booking med sit rum og sin gæst (A-11).
/// </summary>
/// <remarks>
/// Læsestier henter denne i stedet for entiteten. Det fjerner fælden hvor en mapper kaldes
/// på en booking hentet uden navigationsdata og tavst giver tomme felter — og det er hurtigere.
/// <para>
/// Typen er ikke en DTO til UI: den er råt læsemateriale, som
/// <see cref="BookingMapping"/> gør til <see cref="BookingListItemDto"/> eller
/// <see cref="BookingDetailsDto"/>. Grunden til det ekstra trin er
/// <see cref="BookingActionsDto"/>, der afhænger af "i dag" — og datoen kommer fra uret i
/// servicen, ikke fra databasen (A-05).
/// </para>
/// </remarks>
/// <param name="BookingId">Bookingens id.</param>
/// <param name="StartDate">Ankomstdato.</param>
/// <param name="EndDate">Booket afrejsedato.</param>
/// <param name="EffectiveEndDate">
/// Effektiv slutdato (A-01). Projiceres fra databasens genererede kolonne
/// <c>effective_end_date</c>, som beregner samme udtryk som <c>Booking.EffectiveEndDate</c>.
/// </param>
/// <param name="Status">Bookingens livscyklustilstand.</param>
/// <param name="CheckInTime">Faktisk indtjekningstidspunkt, eller <c>null</c>.</param>
/// <param name="CheckOutTime">Faktisk udtjekningstidspunkt, eller <c>null</c>.</param>
/// <param name="CheckOutDate">Hotel-lokal udtjekningsdato, eller <c>null</c>.</param>
/// <param name="Room">Rummet, projiceret med.</param>
/// <param name="Guest">Gæsten, projiceret med og uden pasnummer.</param>
public sealed record BookingReadModel(
    int BookingId,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly EffectiveEndDate,
    BookingStatus Status,
    DateTimeOffset? CheckInTime,
    DateTimeOffset? CheckOutTime,
    DateOnly? CheckOutDate,
    RoomListItemDto Room,
    GuestListItemDto Guest);