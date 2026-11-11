namespace NFHotel.Application.Dto.Pricing;

/// <summary>The occupancy of the same days in one earlier year, which a price suggestion rests on (MIL-071).</summary>
/// <param name="Year">The earlier year.</param>
/// <param name="OccupancyPercent">The occupancy of that year in percent.</param>
public sealed record PriceSuggestionHistoryDto(int Year, decimal OccupancyPercent);
