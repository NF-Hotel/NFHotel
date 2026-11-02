namespace NFHotel.Application.Dto.Pricing;

/// <summary>The pricing settings of the Manager (MIL-055), or the defaults when none are saved.</summary>
/// <param name="SurchargePercent">The surcharge of a high season in percent.</param>
/// <param name="DiscountPercent">The discount of a low season in percent.</param>
/// <param name="HighThresholdPercent">The occupancy at or above which a month is high season.</param>
/// <param name="LowThresholdPercent">The occupancy at or below which a month is low season.</param>
/// <param name="MaxChangePercent">The largest change of a price in percent.</param>
/// <param name="MinimumNightlyPrice">The price a discount never goes below.</param>
/// <param name="DiscountSizes">The room sizes that get a discount: <c>Single</c>, <c>Double</c> or <c>Suite</c>.</param>
/// <param name="SetBy">The user name of the staff member who saved them, or <c>null</c> for the defaults.</param>
/// <param name="SetAt">When they were saved, or <c>null</c> for the defaults.</param>
public sealed record PricingSettingsDto(
    decimal SurchargePercent,
    decimal DiscountPercent,
    decimal HighThresholdPercent,
    decimal LowThresholdPercent,
    decimal MaxChangePercent,
    decimal MinimumNightlyPrice,
    IReadOnlyList<string> DiscountSizes,
    string? SetBy,
    DateTimeOffset? SetAt);
