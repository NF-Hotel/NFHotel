namespace NFHotel.Application.UseCases.Pricing;

/// <summary>The average occupancy of a calendar month over the years of the report, and its season.</summary>
/// <param name="Month">The calendar month, 1 to 12.</param>
/// <param name="OccupancyPercent">The average occupancy in percent; <see langword="null" /> when the report has no figure for the month.</param>
/// <param name="Season">The season of the month; Normal when there is no figure.</param>
public sealed record MonthSeason(int Month, decimal? OccupancyPercent, Season Season);
