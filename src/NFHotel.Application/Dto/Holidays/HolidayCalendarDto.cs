using NFHotel.Application.Ports;

namespace NFHotel.Application.Dto.Holidays;

/// <summary>
/// The holiday calendar as the analysis service has exported it.
/// </summary>
/// <remarks>
/// The source of the service is the Python package <c>holidays</c>; the calendar is an export, not
/// sandheden — <see cref="IHolidayCalendarClient.RefreshAsync"/> genererer den forfra.
/// </remarks>
/// <param name="Country">The country code the calendar applies to, for example <c>KH</c>.</param>
/// <param name="Source">Where the calendar comes from, for example <c>python-holidays 0.104</c>.</param>
/// <param name="FirstYear">The first year the export covers.</param>
/// <param name="LastYear">The last year the export covers.</param>
/// <param name="Holidays">The holidays in chronological order.</param>
public sealed record HolidayCalendarDto(
    string Country,
    string Source,
    int FirstYear,
    int LastYear,
    IReadOnlyList<HolidayPeriodDto> Holidays);