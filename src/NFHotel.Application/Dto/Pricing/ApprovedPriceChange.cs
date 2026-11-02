namespace NFHotel.Application.Dto.Pricing;

/// <summary>The outcome of an approval: the price change, and whether this call created it (MIL-055).</summary>
/// <param name="Change">The price change.</param>
/// <param name="Created"><c>false</c> when the same change already existed and is returned as it was.</param>
public sealed record ApprovedPriceChange(PriceChangeDto Change, bool Created);
