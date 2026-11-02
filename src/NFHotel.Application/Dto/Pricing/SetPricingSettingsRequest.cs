namespace NFHotel.Application.Dto.Pricing;

/// <summary>The body that saves the pricing settings (MIL-051). The numbers are rounded to two decimals.</summary>
/// <param name="SurchargePercent">The surcharge of a high season in percent, from 0 to 100.</param>
/// <param name="DiscountPercent">The discount of a low season in percent, from 0 to 100.</param>
/// <param name="HighThresholdPercent">The occupancy at or above which a month is high season, from 0 to 100.</param>
/// <param name="LowThresholdPercent">The occupancy at or below which a month is low season, below the high threshold.</param>
/// <param name="MaxChangePercent">The largest change of a price in percent, above 0 and at most 100.</param>
/// <param name="MinimumNightlyPrice">The price a discount never goes below, from 0 to 1,000,000.</param>
/// <param name="DiscountSizes">The room sizes that get a discount, at least one: <c>Single</c>, <c>Double</c>, <c>Suite</c>, in any letter case.</param>
public sealed record SetPricingSettingsRequest(
    decimal SurchargePercent,
    decimal DiscountPercent,
    decimal HighThresholdPercent,
    decimal LowThresholdPercent,
    decimal MaxChangePercent,
    decimal MinimumNightlyPrice,
    IReadOnlyList<string> DiscountSizes);
