namespace NFHotel.Application.Dto.Pricing;

/// <summary>An open price suggestion with the figures it rests on (MIL-051).</summary>
/// <param name="Key">A stable text that names the suggestion; the approval sends it back and the server recomputes the rest.</param>
/// <param name="Size">The room size: <c>Single</c>, <c>Double</c> or <c>Suite</c>.</param>
/// <param name="Season">The season: <c>High</c> for a surcharge, <c>Low</c> for a discount.</param>
/// <param name="FirstDay">The first day of the period.</param>
/// <param name="LastDay">The last day of the period.</param>
/// <param name="Percentage">The change in percent: above 0 for a surcharge, below 0 for a discount.</param>
/// <param name="OldPrice">The base rate.</param>
/// <param name="NewPrice">The suggested nightly price.</param>
/// <param name="OccupancyPercent">The average occupancy of the months of the period.</param>
/// <param name="ThresholdPercent">The threshold the occupancy crossed.</param>
/// <param name="WasLimited">True when the largest change or the minimum price cut the result.</param>
public sealed record PriceSuggestionItemDto(
    string Key,
    string Size,
    string Season,
    DateOnly FirstDay,
    DateOnly LastDay,
    decimal Percentage,
    decimal OldPrice,
    decimal NewPrice,
    decimal OccupancyPercent,
    decimal ThresholdPercent,
    bool WasLimited);
