namespace NFHotel.Application.Dto.Holidays;

/// <summary>
/// En helligdag eller helligdagsperiode, fx "Khmer New Year's Day" 13.–16. april.
/// </summary>
/// <remarks>
/// Ingen formatering: datoer er datoer. En enkeltstående helligdag har
/// <paramref name="StartDate"/> lig <paramref name="EndDate"/>.
/// </remarks>
/// <param name="Name">Helligdagens navn, som analysetjenesten leverer det.</param>
/// <param name="StartDate">Første dag i perioden.</param>
/// <param name="EndDate">Sidste dag i perioden (inklusive).</param>
public sealed record HolidayPeriodDto(string Name, DateOnly StartDate, DateOnly EndDate);