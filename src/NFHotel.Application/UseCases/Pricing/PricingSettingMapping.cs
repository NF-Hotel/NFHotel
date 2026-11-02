using NFHotel.Domain.Entities;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>The one mapping from the stored pricing settings to the settings of the rule (MIL-055).</summary>
public static class PricingSettingMapping
{
    /// <summary>Maps the stored settings to the record the pricing rule reads.</summary>
    /// <param name="setting">The stored settings.</param>
    /// <returns>The settings of the rule; what the entity accepts, <see cref="PricingSettings.EnsureValid"/> accepts too.</returns>
    public static PricingSettings ToRule(this PricingSetting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);

        return new PricingSettings(
            setting.SurchargePercent,
            setting.DiscountPercent,
            setting.HighThresholdPercent,
            setting.LowThresholdPercent,
            setting.MaxChangePercent,
            setting.MinimumNightlyPrice,
            setting.DiscountSizes);
    }
}
