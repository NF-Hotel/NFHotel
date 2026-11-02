using NFHotel.Domain.Entities;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>
/// The one place that knows how a percentage turns a base rate into a price (MIL-051). The suggestion rule, the approval and
/// the effective price all use it, so the price the page announces is the price the booking gets.
/// </summary>
internal static class PriceMath
{
    private const decimal Hundred = 100m;
    private const decimal TenThousand = 10_000m;
    private const int MoneyDecimals = 2;

    /// <summary>Applies a signed percentage to a price, rounded to two decimals away from zero.</summary>
    /// <param name="price">The base rate.</param>
    /// <param name="percentage">The change in percent: above 0 for a surcharge, below 0 for a discount.</param>
    /// <returns>The price with the change.</returns>
    public static decimal ApplyPercentage(decimal price, decimal percentage) =>
        Math.Round(price * (1m + (percentage / Hundred)), MoneyDecimals, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Finds the largest discount, in percent with the decimals that a <see cref="PriceChange"/> keeps, that still gives a price
    /// of at least <paramref name="floor"/>. It is rounded down, so the price is never below the floor; it can be above the
    /// floor by less than a hundredth of a percent of the rate.
    /// </summary>
    /// <param name="rate">The base rate.</param>
    /// <param name="floor">The lowest price that is allowed, below <paramref name="rate"/>.</param>
    /// <returns>The discount as a positive number; 0 when there is no room for a discount.</returns>
    public static decimal LargestDiscountKeeping(decimal rate, decimal floor)
    {
        if (rate <= 0m || floor >= rate)
        {
            return 0m;
        }

        // Multiply before dividing so that an exact result stays exact.
        var hundredths = Math.Floor((rate - floor) * TenThousand / rate);

        return hundredths / Hundred;
    }
}
