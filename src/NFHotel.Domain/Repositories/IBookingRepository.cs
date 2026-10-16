using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Repositories;

/// <summary>
/// Dataadgang for bookinger — entitetsdelen. Implementeres i Infrastructure.
/// </summary>
/// <remarks>
/// Repositoryet <b>beslutter ikke</b>. Vil servicen vide hvilke bookinger der spærrer et
/// rum, sender den selv statussættet med — hentet fra <see cref="NFHotel.Domain.ValueObjects.BookingStatusExtensions.BlocksRoom"/>.
/// Reglen står dermed ét sted i stedet for i SQL. Læsning af projektioner ligger i
/// <c>IBookingQueries</c> (Application), fordi Domain ikke kender DTO'er (ADR-0001).
/// </remarks>
public interface IBookingRepository : IRepository<Booking, int>
{
    /// <summary>
    /// Henter de bookinger der <i>kan</i> være i konflikt med en ønsket periode på et rum (BR-19, B-03).
    /// </summary>
    /// <param name="roomId">Rummet den ønskede periode gælder.</param>
    /// <param name="periodStart">Ønsket ankomstdato.</param>
    /// <param name="periodEnd">Ønsket afrejsedato, eksklusiv.</param>
    /// <param name="blockingStatuses">
    /// De statusser der spærrer. Kommer fra <see cref="NFHotel.Domain.ValueObjects.BookingStatusExtensions.BlocksRoom"/> — ikke fra
    /// et hardkodet <c>WHERE</c>.
    /// </param>
    /// <param name="excludeBookingId">
    /// Bookingen der redigeres, så den ikke blokerer sig selv. <c>null</c> ved oprettelse.
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// <b>Kandidater</b>, ikke konflikter. Grovfiltreringen på rum, statussæt og datovindue
    /// sker i SQL; selve afgørelsen træffes af <see cref="NFHotel.Domain.Entities.Booking.ConflictsWith"/>, så
    /// repository og domæne aldrig kan blive uenige om <c>&lt;</c> mod <c>&lt;=</c> (A-04).
    /// </returns>
    Task<IReadOnlyList<Booking>> GetOverlapCandidatesAsync(
        int roomId,
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyCollection<BookingStatus> blockingStatuses,
        int? excludeBookingId,
        CancellationToken cancellationToken = default);
}