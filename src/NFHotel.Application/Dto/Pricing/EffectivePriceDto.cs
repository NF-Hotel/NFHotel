namespace NFHotel.Application.Dto.Pricing;

/// <summary>The price of a room size on a day: the base rate with the price change that counts on that day (MIL-055).</summary>
/// <param name="Size">The room size.</param>
/// <param name="Day">The day.</param>
/// <param name="BaseRate">The nightly base rate, or <c>null</c> when none is set.</param>
/// <param name="Price">The price on the day, rounded to two decimals, or <c>null</c> when there is no base rate.</param>
/// <param name="Percentage">The percentage of the change that counts on the day, or <c>null</c> when none does.</param>
public sealed record EffectivePriceDto(string Size, DateOnly Day, decimal? BaseRate, decimal? Price, decimal? Percentage);
