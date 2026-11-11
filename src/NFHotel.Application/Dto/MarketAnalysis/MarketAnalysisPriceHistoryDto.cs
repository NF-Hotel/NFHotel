namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>The occupancy of the same holiday or week in one earlier year (result schema 1.3).</summary>
/// <param name="Year">The earlier year.</param>
/// <param name="Occupancy">The share of room nights taken, from 0 to 1.</param>
public sealed record MarketAnalysisPriceHistoryDto(int Year, double Occupancy);
