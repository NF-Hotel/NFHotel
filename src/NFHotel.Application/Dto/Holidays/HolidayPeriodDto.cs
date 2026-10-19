namespace NFHotel.Application.Dto.Holidays;

/// <summary>
/// A holiday or holiday period, for example "Khmer New Year's Day" 13–16 April.
/// </summary>
/// <remarks>
/// No formatting: dates are dates. A single holiday has
/// <paramref name="StartDate"/> lig <paramref name="EndDate"/>.
/// </remarks>
/// <param name="Name">The name of the holiday, as the analysis service delivers it.</param>
/// <param name="StartDate">The first day of the period.</param>
/// <param name="EndDate">Last day of the period (inclusive).</param>
public sealed record HolidayPeriodDto(string Name, DateOnly StartDate, DateOnly EndDate);
