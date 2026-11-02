namespace NFHotel.Domain.Errors;

/// <summary>
/// Error codes of the pricing settings of the Manager (MIL-055). A code is a stable string; the text for the user is the
/// concern of the presentation layer.
/// </summary>
public static class PricingSettingErrors
{
    /// <summary>The surcharge is not from 0 to 100 percent.</summary>
    public const string SurchargeInvalid = "pricing_setting.surcharge_invalid";

    /// <summary>The discount is not from 0 to 100 percent.</summary>
    public const string DiscountInvalid = "pricing_setting.discount_invalid";

    /// <summary>The high threshold is not from 0 to 100 percent.</summary>
    public const string HighThresholdInvalid = "pricing_setting.high_threshold_invalid";

    /// <summary>The low threshold is not from 0 to 100 percent.</summary>
    public const string LowThresholdInvalid = "pricing_setting.low_threshold_invalid";

    /// <summary>The low threshold is not below the high threshold.</summary>
    public const string ThresholdsOrderInvalid = "pricing_setting.thresholds_order_invalid";

    /// <summary>The largest change is not above 0 and at most 100 percent.</summary>
    public const string MaxChangeInvalid = "pricing_setting.max_change_invalid";

    /// <summary>The minimum nightly price is below 0 or above the largest price.</summary>
    public const string MinimumPriceInvalid = "pricing_setting.minimum_price_invalid";

    /// <summary>No room size is chosen for the discount, or a size is not one of the known ones.</summary>
    public const string DiscountSizesInvalid = "pricing_setting.discount_sizes_invalid";

    /// <summary>The name of the person who saved the settings is missing or too long.</summary>
    public const string SetByInvalid = "pricing_setting.set_by_invalid";

    /// <summary>Another first save of the settings came first; the Manager reloads and tries again.</summary>
    public const string ConcurrencyConflict = "pricing_setting.concurrency_conflict";
}
