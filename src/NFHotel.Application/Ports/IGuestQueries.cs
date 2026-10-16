using NFHotel.Application.Dto.Guests;

namespace NFHotel.Application.Ports;

/// <summary>
/// Læsning af gæster som projektioner (A-11). Implementeres i Infrastructure.
/// </summary>
/// <remarks>
/// Entiteter hentes kun på kommandostier, via <c>IGuestRepository</c> i Domain.
/// </remarks>
public interface IGuestQueries
{
    /// <summary>
    /// Henter en gæsts detaljer som projektion.
    /// </summary>
    /// <param name="guestId">Gæstens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Detaljerne, eller <c>null</c> hvis gæsten ikke findes (BR-115).</returns>
    Task<GuestDetailsDto?> GetDetailsAsync(int guestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Søger gæster (BR-68, BR-N-05).
    /// </summary>
    /// <param name="searchTerms">
    /// Søgeordene. <b>HVERT</b> ord skal findes (case-insensitivt) i enten fornavn,
    /// efternavn, land eller e-mail — altså AND mellem ord, OR mellem felter. Et tomt sæt
    /// returnerer alle gæster (BR-70).
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>De matchende gæster som listerækker.</returns>
    /// <remarks>
    /// Semantikken er konsolideret fra to uenige søgninger i det gamle system: repoet søgte
    /// kun på for- og efternavn (BR-116), mens ViewModel'en delte teksten i ord og krævede
    /// at alle ord fandtes i navn, land eller e-mail (BR-68). BR-68 vandt (BR-N-05).
    /// Tokeniseringen er reglen og sker i servicen; matchningen er dataadgang og sker her.
    /// Kontrakten skal pinnes af en test i Infrastructure.Tests, ellers driver de fra hinanden.
    /// </remarks>
    Task<IReadOnlyList<GuestListItemDto>> SearchAsync(
        IReadOnlyCollection<string> searchTerms,
        CancellationToken cancellationToken = default);
}