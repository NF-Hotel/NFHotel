using NFHotel.Application.Ports;

namespace NFHotel.Application.Dto.Holidays;

/// <summary>
/// Helligdagskalenderen som analysetjenesten har eksporteret den.
/// </summary>
/// <remarks>
/// Tjenestens kilde er Python-pakken <c>holidays</c>; kalenderen er en eksport, ikke
/// sandheden — <see cref="IHolidayCalendarClient.RefreshAsync"/> genererer den forfra.
/// </remarks>
/// <param name="Country">Landekoden kalenderen gælder for, fx <c>KH</c>.</param>
/// <param name="Source">Hvor kalenderen stammer fra, fx <c>python-holidays 0.104</c>.</param>
/// <param name="FirstYear">Første år eksporten dækker.</param>
/// <param name="LastYear">Sidste år eksporten dækker.</param>
/// <param name="Holidays">Helligdagene i kronologisk rækkefølge.</param>
public sealed record HolidayCalendarDto(
    string Country,
    string Source,
    int FirstYear,
    int LastYear,
    IReadOnlyList<HolidayPeriodDto> Holidays);