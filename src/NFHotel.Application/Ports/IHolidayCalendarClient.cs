using NFHotel.Application.Dto.Holidays;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;

namespace NFHotel.Application.Ports;

/// <summary>
/// Adgang til helligdagskalenderen i analysetjenesten (NF Hotel Analytics API).
/// </summary>
/// <remarks>
/// Kontrakten ligger i Application, implementeringen (HTTP) i Infrastructure — samme
/// afhængighedsretning som repositories. Application kender derfor hverken
/// <c>HttpClient</c> eller tjenestens URL og nøgle.
/// <para>
/// Alle forventede fejl — tjenesten er nede, nøglen afvises, svaret er ubrugeligt — er
/// <see cref="Result"/>-udfald med en kode fra <see cref="ErrorCodes.Holidays"/>. Kun
/// programmørfejl (fx en <c>null</c>-parameter) og annullering kastes som exceptions.
/// </para>
/// </remarks>
public interface IHolidayCalendarClient
{
    /// <summary>
    /// Henter den eksporterede helligdagskalender.
    /// </summary>
    /// <param name="year">
    /// Begræns til ét år, eller <c>null</c> for hele eksporten. Kalenderens
    /// <c>FirstYear</c> og <c>LastYear</c> beskriver stadig hele eksporten.
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// Kalenderen, eller en fejl fra <see cref="ErrorCodes.Holidays"/>.
    /// </returns>
    Task<Result<HolidayCalendarDto>> GetAsync(int? year = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Får tjenesten til at generere helligdagskalenderen forfra fra sin kilde.
    /// </summary>
    /// <remarks>
    /// Ændrer tilstand hos tjenesten (den overskriver sin eksportfil). Brug den når
    /// kilden er opdateret eller et nyt år begynder — ikke ved hver visning.
    /// </remarks>
    /// <param name="firstYear">Første år, eller <c>null</c> for tjenestens standard (2022).</param>
    /// <param name="lastYear">Sidste år, eller <c>null</c> for tjenestens standard (næste år).</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// Den nye kalender, eller en fejl fra <see cref="ErrorCodes.Holidays"/>.
    /// <see cref="ErrorCodes.Holidays.InvalidRequest"/> hvis <paramref name="firstYear"/>
    /// ligger efter <paramref name="lastYear"/>.
    /// </returns>
    Task<Result<HolidayCalendarDto>> RefreshAsync(
        int? firstYear = null,
        int? lastYear = null,
        CancellationToken cancellationToken = default);
}