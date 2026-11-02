using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// The pricing settings of the Manager (MIL-055): how much a price changes in a high or a low season, when a month is high
/// or low, and which room sizes get a discount. There is at most one row; <see cref="Slot"/> carries a unique index.
/// </summary>
public sealed class PricingSetting : Entity<int>
{
    /// <summary>The value of <see cref="Slot"/> of the one row.</summary>
    public const int SingletonSlot = 1;

    /// <summary>The longest user name that is stored.</summary>
    public const int MaxTextLength = 100;

    /// <summary>The largest percentage.</summary>
    public const double MaxPercent = 100;

    /// <summary>The largest minimum nightly price.</summary>
    public const double MaxMinimumPrice = BookingMarketDetail.MaxPricePerNight;

    /// <summary>The number of decimals the numbers are kept with.</summary>
    public const int Decimals = 2;

    private PricingSetting()
    {
    }

    /// <summary>The fixed key value that makes a second row impossible.</summary>
    public int Slot { get; private set; } = SingletonSlot;

    /// <summary>The surcharge of a high season in percent, from 0 to 100.</summary>
    [Range(0.0, MaxPercent, ErrorMessage = PricingSettingErrors.SurchargeInvalid)]
    public decimal SurchargePercent { get; private set; }

    /// <summary>The discount of a low season in percent, from 0 to 100.</summary>
    [Range(0.0, MaxPercent, ErrorMessage = PricingSettingErrors.DiscountInvalid)]
    public decimal DiscountPercent { get; private set; }

    /// <summary>The occupancy in percent at or above which a month is high season, from 0 to 100.</summary>
    [Range(0.0, MaxPercent, ErrorMessage = PricingSettingErrors.HighThresholdInvalid)]
    public decimal HighThresholdPercent { get; private set; }

    /// <summary>The occupancy in percent at or below which a month is low season, from 0 to 100 and below the high threshold.</summary>
    [Range(0.0, MaxPercent, ErrorMessage = PricingSettingErrors.LowThresholdInvalid)]
    public decimal LowThresholdPercent { get; private set; }

    /// <summary>The largest change of a price in percent, above 0 and at most 100.</summary>
    [Range(0.01, MaxPercent, ErrorMessage = PricingSettingErrors.MaxChangeInvalid)]
    public decimal MaxChangePercent { get; private set; }

    /// <summary>The price a discount never goes below, from 0 to <see cref="MaxMinimumPrice"/>.</summary>
    [Range(0.0, MaxMinimumPrice, ErrorMessage = PricingSettingErrors.MinimumPriceInvalid)]
    public decimal MinimumNightlyPrice { get; private set; }

    /// <summary>The room sizes that get a discount in a low season: at least one, each once, in the order of the sizes.</summary>
    public IReadOnlyList<RoomSize> DiscountSizes { get; private set; } = [];

    /// <summary>The user name of the staff member who saved the settings.</summary>
    [Required(ErrorMessage = PricingSettingErrors.SetByInvalid)]
    [StringLength(MaxTextLength, ErrorMessage = PricingSettingErrors.SetByInvalid)]
    public string SetBy { get; private set; } = string.Empty;

    /// <summary>When the settings were saved.</summary>
    public DateTimeOffset SetAt { get; private set; }

    /// <summary>Creates the settings. The numbers are rounded to two decimals.</summary>
    /// <param name="surchargePercent">The surcharge of a high season in percent.</param>
    /// <param name="discountPercent">The discount of a low season in percent.</param>
    /// <param name="highThresholdPercent">The occupancy at or above which a month is high season.</param>
    /// <param name="lowThresholdPercent">The occupancy at or below which a month is low season.</param>
    /// <param name="maxChangePercent">The largest change of a price in percent.</param>
    /// <param name="minimumNightlyPrice">The price a discount never goes below.</param>
    /// <param name="discountSizes">The room sizes that get a discount.</param>
    /// <param name="setBy">Who saves them.</param>
    /// <param name="setAt">When they are saved.</param>
    /// <returns>The new settings.</returns>
    /// <exception cref="DomainException">A field breaks an invariant.</exception>
    public static PricingSetting Create(
        decimal surchargePercent,
        decimal discountPercent,
        decimal highThresholdPercent,
        decimal lowThresholdPercent,
        decimal maxChangePercent,
        decimal minimumNightlyPrice,
        IEnumerable<RoomSize> discountSizes,
        string setBy,
        DateTimeOffset setAt)
    {
        var created = new PricingSetting();
        created.Apply(
            surchargePercent, discountPercent, highThresholdPercent, lowThresholdPercent, maxChangePercent, minimumNightlyPrice,
            discountSizes, setBy, setAt);

        return created;
    }

    /// <summary>Replaces the settings.</summary>
    /// <param name="surchargePercent">The surcharge of a high season in percent.</param>
    /// <param name="discountPercent">The discount of a low season in percent.</param>
    /// <param name="highThresholdPercent">The occupancy at or above which a month is high season.</param>
    /// <param name="lowThresholdPercent">The occupancy at or below which a month is low season.</param>
    /// <param name="maxChangePercent">The largest change of a price in percent.</param>
    /// <param name="minimumNightlyPrice">The price a discount never goes below.</param>
    /// <param name="discountSizes">The room sizes that get a discount.</param>
    /// <param name="setBy">Who changes them.</param>
    /// <param name="setAt">When they are changed.</param>
    /// <exception cref="DomainException">A field breaks an invariant; the settings are not changed.</exception>
    public void Change(
        decimal surchargePercent,
        decimal discountPercent,
        decimal highThresholdPercent,
        decimal lowThresholdPercent,
        decimal maxChangePercent,
        decimal minimumNightlyPrice,
        IEnumerable<RoomSize> discountSizes,
        string setBy,
        DateTimeOffset setAt) =>
        Apply(
            surchargePercent, discountPercent, highThresholdPercent, lowThresholdPercent, maxChangePercent, minimumNightlyPrice,
            discountSizes, setBy, setAt);

    // Plain loops: a lambda here would add a compiler-generated type to the entities, which the structure test refuses.
    private static RoomSize[] Canonical(IEnumerable<RoomSize>? sizes)
    {
        var set = new SortedSet<RoomSize>(sizes ?? []);

        return [.. set];
    }

    private static bool AllDefined(RoomSize[] sizes)
    {
        foreach (var size in sizes)
        {
            if (!Enum.IsDefined(size))
            {
                return false;
            }
        }

        return true;
    }

    private static decimal Round(decimal value) => Math.Round(value, Decimals, MidpointRounding.AwayFromZero);

    private void Apply(
        decimal surchargePercent,
        decimal discountPercent,
        decimal highThresholdPercent,
        decimal lowThresholdPercent,
        decimal maxChangePercent,
        decimal minimumNightlyPrice,
        IEnumerable<RoomSize> discountSizes,
        string setBy,
        DateTimeOffset setAt)
    {
        var sizes = Canonical(discountSizes);
        var candidate = new PricingSetting
        {
            SurchargePercent = Round(surchargePercent),
            DiscountPercent = Round(discountPercent),
            HighThresholdPercent = Round(highThresholdPercent),
            LowThresholdPercent = Round(lowThresholdPercent),
            MaxChangePercent = Round(maxChangePercent),
            MinimumNightlyPrice = Round(minimumNightlyPrice),
            DiscountSizes = sizes,
            SetBy = setBy?.Trim() ?? string.Empty,
            SetAt = setAt.ToUniversalTime(),
        };

        var codes = new List<string>();
        try
        {
            EntityValidation.ThrowIfInvalid(candidate);
        }
        catch (DomainException exception)
        {
            codes.AddRange(exception.Codes);
        }

        // The order is judged only when both thresholds are valid on their own; otherwise the range code says it better.
        if (!codes.Contains(PricingSettingErrors.HighThresholdInvalid)
            && !codes.Contains(PricingSettingErrors.LowThresholdInvalid)
            && candidate.LowThresholdPercent >= candidate.HighThresholdPercent)
        {
            codes.Add(PricingSettingErrors.ThresholdsOrderInvalid);
        }

        if (sizes.Length == 0 || !AllDefined(sizes))
        {
            codes.Add(PricingSettingErrors.DiscountSizesInvalid);
        }

        if (codes.Count > 0)
        {
            throw new DomainException(codes.Distinct().ToArray());
        }

        SurchargePercent = candidate.SurchargePercent;
        DiscountPercent = candidate.DiscountPercent;
        HighThresholdPercent = candidate.HighThresholdPercent;
        LowThresholdPercent = candidate.LowThresholdPercent;
        MaxChangePercent = candidate.MaxChangePercent;
        MinimumNightlyPrice = candidate.MinimumNightlyPrice;
        DiscountSizes = candidate.DiscountSizes;
        SetBy = candidate.SetBy;
        SetAt = candidate.SetAt;
    }
}
