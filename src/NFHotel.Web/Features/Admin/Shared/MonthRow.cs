namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>One calendar year of a month grid: a value or <c>null</c> for each of the twelve months.</summary>
/// <param name="Year">The year.</param>
/// <param name="Values">Twelve values, January first; <c>null</c> where the series has no point for that month.</param>
/// <param name="SmallSamples">Twelve flags: true where the value rests on too few bookings.</param>
public sealed record MonthRow(int Year, IReadOnlyList<double?> Values, IReadOnlyList<bool> SmallSamples);
