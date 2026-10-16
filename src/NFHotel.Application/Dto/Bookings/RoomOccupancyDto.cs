using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// Én bookings beslaglæggelse af et rum, til tilgængelighedsopslaget.
/// </summary>
/// <remarks>
/// Bevidst så smal som muligt: kun det <see cref="NFHotel.Domain.Entities.Booking"/> skal bruge for at afgøre
/// om rummet er optaget. Alternativet — at hente alle bookinger i perioden som entiteter og
/// filtrere i hukommelsen — er præcis den fejl BR-111 beskriver.
/// <para>
/// Bemærk at overlapsafgørelsen <b>ikke</b> træffes her og heller ikke i SQL: servicen
/// bygger en <c>DateRange</c> af felterne og spørger domænet (A-01, A-04). Ellers ville
/// prædikatet findes to steder og kunne blive uenige om <c>&lt;</c> mod <c>&lt;=</c>.
/// </para>
/// </remarks>
/// <param name="BookingId">Bookingens id, så en booking kan udelades ved redigering.</param>
/// <param name="RoomId">Rummet der er lagt beslag på.</param>
/// <param name="StartDate">Første optagne nat.</param>
/// <param name="EffectiveEndDate">Første ledige dato efter beslaglæggelsen (A-01).</param>
/// <param name="Status">Bookingens status, så <c>BookingStatusExtensions.BlocksRoom</c> kan efterprøves.</param>
public sealed record RoomOccupancyDto(
    int BookingId,
    int RoomId,
    DateOnly StartDate,
    DateOnly EffectiveEndDate,
    BookingStatus Status);