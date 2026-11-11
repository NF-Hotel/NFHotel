namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>A period for which the service made no suggestion for want of earlier years (result schema 1.3).</summary>
/// <param name="FirstDate">The first day of the period.</param>
/// <param name="LastDate">The last day of the period.</param>
/// <param name="Holiday">The name of the holiday, or <c>null</c> for a week.</param>
/// <param name="YearsFound">How many earlier years the bookings hold for the period.</param>
public sealed record MarketAnalysisPriceGapDto(DateOnly FirstDate, DateOnly LastDate, string? Holiday, int YearsFound);
