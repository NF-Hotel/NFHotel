namespace NFHotel.Application.UseCases.Pricing;

/// <summary>The occupancy of one month in the market analysis report.</summary>
/// <param name="Year">The year of the month.</param>
/// <param name="Month">The month, 1 to 12.</param>
/// <param name="OccupancyPercent">The occupancy in percent, 0 to 100.</param>
public sealed record OccupancyObservation(int Year, int Month, decimal OccupancyPercent);
