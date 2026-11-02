namespace NFHotel.Application.UseCases.Pricing;

/// <summary>The seasons of the twelve calendar months, or the fact that the report is too thin to tell.</summary>
/// <param name="HasBasis">True when the report covers all twelve calendar months.</param>
/// <param name="Months">The twelve calendar months, January first.</param>
public sealed record CalendarSeasons(bool HasBasis, IReadOnlyList<MonthSeason> Months);
