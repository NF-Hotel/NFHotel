namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>
/// What the website asks the market analysis service for besides the analyses: price suggestions for the next weeks
/// (<c>pricing</c> of <c>RunRequest</c>, result schema 1.3). The service works them out by a fixed rule from these settings;
/// the website never works them out itself (MIL-071).
/// </summary>
/// <param name="HighOccupancyThreshold">The share of room nights, from 0 to 1, at or above which a surcharge is suggested.</param>
/// <param name="LowOccupancyThreshold">The share of room nights, from 0 to 1, at or below which a discount is suggested.</param>
/// <param name="SurchargePercent">The surcharge in percent.</param>
/// <param name="DiscountPercent">The discount in percent.</param>
/// <param name="DiscountRoomSizes">The room sizes that qualify for a discount, in lower case.</param>
public sealed record MarketAnalysisPricingRequest(
    decimal HighOccupancyThreshold,
    decimal LowOccupancyThreshold,
    decimal SurchargePercent,
    decimal DiscountPercent,
    IReadOnlyList<string> DiscountRoomSizes);
