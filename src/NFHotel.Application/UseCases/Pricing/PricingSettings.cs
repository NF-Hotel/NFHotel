using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>The settings of the pricing rule: how much to change a price, and when a month is high or low season.</summary>
/// <param name="SurchargePercent">The surcharge of a high season, in percent.</param>
/// <param name="DiscountPercent">The discount of a low season, in percent.</param>
/// <param name="HighThreshold">The occupancy in percent at or above which a month is high season.</param>
/// <param name="LowThreshold">The occupancy in percent at or below which a month is low season.</param>
/// <param name="MaxChangePercent">The largest change of a price that is suggested, in percent.</param>
/// <param name="MinimumNightlyPrice">The price that a discount never goes below.</param>
/// <param name="DiscountSizesOrNull">The room sizes that get a discount in a low season; only a single room when null.</param>
public sealed record PricingSettings(
    decimal SurchargePercent = 20m,
    decimal DiscountPercent = 15m,
    decimal HighThreshold = 80m,
    decimal LowThreshold = 40m,
    decimal MaxChangePercent = 30m,
    decimal MinimumNightlyPrice = 0m,
    IReadOnlyList<RoomSize>? DiscountSizesOrNull = null)
{
    // The limits are the ones of the entity, so the rule and the stored settings cannot drift apart.
    private const decimal MaxPercent = (decimal)PricingSetting.MaxPercent;

    private static readonly RoomSize[] DefaultDiscountSizes = [RoomSize.Single];

    /// <summary>The settings that apply until the Manager changes them.</summary>
    public static PricingSettings Defaults { get; } = new();

    /// <summary>The room sizes that get a discount in a low season.</summary>
    public IReadOnlyList<RoomSize> DiscountSizes => DiscountSizesOrNull ?? DefaultDiscountSizes;

    /// <summary>Guards the rule against settings that make no sense.</summary>
    /// <exception cref="ArgumentException">The thresholds, a percentage or the minimum price is out of range.</exception>
    public void EnsureValid()
    {
        if (LowThreshold >= HighThreshold)
        {
            throw new ArgumentException("The low threshold must be below the high threshold.", nameof(LowThreshold));
        }

        EnsurePercent(SurchargePercent, nameof(SurchargePercent));
        EnsurePercent(DiscountPercent, nameof(DiscountPercent));
        EnsurePercent(HighThreshold, nameof(HighThreshold));
        EnsurePercent(LowThreshold, nameof(LowThreshold));

        if (MaxChangePercent is <= 0m or > MaxPercent)
        {
            throw new ArgumentException("The largest change must be above 0 and at most 100 percent.", nameof(MaxChangePercent));
        }

        if (MinimumNightlyPrice < 0m)
        {
            throw new ArgumentException("The minimum price cannot be negative.", nameof(MinimumNightlyPrice));
        }
    }

    private static void EnsurePercent(decimal value, string name)
    {
        if (value is < 0m or > MaxPercent)
        {
            throw new ArgumentException("A percentage must be from 0 to 100.", name);
        }
    }
}
