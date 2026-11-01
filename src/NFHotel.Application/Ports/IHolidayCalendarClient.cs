using NFHotel.Application.Dto.Holidays;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;

namespace NFHotel.Application.Ports;

/// <summary>
/// Access to the holiday calendar in the analysis service (NF Hotel Analytics API).
/// </summary>
/// <remarks>
/// The contract is in Application, the implementation (HTTP) in Infrastructure — the same
/// dependency direction as the repositories. Application therefore knows neither
/// <c>HttpClient</c> nor the URL and key of the service.
/// <para>
/// All expected errors — the service is down, the key is rejected, the answer is unusable — are
/// <see cref="Result"/> outcomes with a code from <see cref="ErrorCodes.Holidays"/>. Only
/// programmer errors (for example a <c>null</c> parameter) and cancellation are thrown as exceptions.
/// </para>
/// </remarks>
public interface IHolidayCalendarClient
{
    /// <summary>
    /// Fetches the exported holiday calendar.
    /// </summary>
    /// <param name="year">
    /// Limit to one year, or <c>null</c> for the whole export. The calendar's
    /// <c>FirstYear</c> and <c>LastYear</c> still describe the whole export.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The calendar, or an error from <see cref="ErrorCodes.Holidays"/>.
    /// </returns>
    Task<Result<HolidayCalendarDto>> GetAsync(int? year = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes the service generate the holiday calendar from scratch from its source.
    /// </summary>
    /// <remarks>
    /// Changes state in the service (it overwrites its export file). Use it when
    /// the source has been updated or a new year begins — not on every display.
    /// </remarks>
    /// <param name="firstYear">First year, or <c>null</c> for the service default (2022).</param>
    /// <param name="lastYear">Last year, or <c>null</c> for the service default (next year).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The new calendar, or an error from <see cref="ErrorCodes.Holidays"/>.
    /// <see cref="ErrorCodes.Holidays.InvalidRequest"/> if <paramref name="firstYear"/>
    /// is after <paramref name="lastYear"/>.
    /// </returns>
    Task<Result<HolidayCalendarDto>> RefreshAsync(
        int? firstYear = null,
        int? lastYear = null,
        CancellationToken cancellationToken = default);
}
