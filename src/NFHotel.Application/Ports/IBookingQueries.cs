using NFHotel.Application.Dto.Bookings;
using NFHotel.Domain.Rules;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Ports;

/// <summary>
/// Læsning af bookinger som projektioner (A-11). Implementeres i Infrastructure.
/// </summary>
/// <remarks>
/// Entiteter hentes kun på kommandostier, via <c>IBookingRepository</c> i Domain; her
/// returneres DTO'er og læsemodeller, som Domain ikke må kende (ADR-0001).
/// </remarks>
public interface IBookingQueries
{
    /// <summary>
    /// Henter én bookings læsemodel med rum og gæst.
    /// </summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Læsemodellen, eller <c>null</c> hvis bookingen ikke findes (BR-115).</returns>
    Task<BookingReadModel?> GetReadModelAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter bookingoversigten for et datovindue (BR-23, BR-24, BR-25).
    /// </summary>
    /// <param name="periodStart">Vinduets første dato, inklusiv.</param>
    /// <param name="periodEnd">Vinduets sidste dato, eksklusiv.</param>
    /// <param name="includedStatuses">
    /// De statusser der skal med. Kalderen vælger — repositoryet hardkoder ikke
    /// "uden annullerede".
    /// </param>
    /// <param name="searchText">
    /// Fritekst på gæstenavn eller værelsesnummer, eller <c>null</c> for alle. Filtreres i
    /// databasen, ikke i hukommelsen (BR-111).
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>De bookinger hvis effektive periode rører vinduet.</returns>
    Task<IReadOnlyList<BookingReadModel>> GetOverviewAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyCollection<BookingStatus> includedStatuses,
        string? searchText,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter beslaglæggelserne i et datovindue, til tilgængelighedsopslaget (BR-34, BR-39).
    /// </summary>
    /// <param name="periodStart">Vinduets første dato, inklusiv.</param>
    /// <param name="periodEnd">Vinduets sidste dato, eksklusiv.</param>
    /// <param name="blockingStatuses">
    /// De statusser der spærrer et rum. Kommer fra <see cref="BookingRules.BlocksRoom"/>.
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Grovfiltrerede beslaglæggelser. Den endelige overlapsafgørelse træffes af domænet.</returns>
    /// <remarks>
    /// Returnerer projektioner og ikke entiteter, fordi kaldet kun bruges som negation i
    /// tilgængelighedsopslaget. At hente hele tabellen og filtrere i hukommelsen er præcis
    /// BR-111's fejl.
    /// </remarks>
    Task<IReadOnlyList<RoomOccupancyDto>> GetOccupancyAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyCollection<BookingStatus> blockingStatuses,
        CancellationToken cancellationToken = default);
}