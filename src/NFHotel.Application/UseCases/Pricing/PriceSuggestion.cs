using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>A suggested price for a room size over a period, with the figures it is based on.</summary>
/// <param name="Size">The room size.</param>
/// <param name="Season">High for a surcharge, Low for a discount.</param>
/// <param name="FirstDay">The first day of the period.</param>
/// <param name="LastDay">The last day of the period.</param>
/// <param name="Percentage">The change in percent, a positive number; the season gives the sign.</param>
/// <param name="OldPrice">The base rate.</param>
/// <param name="NewPrice">The suggested nightly price.</param>
/// <param name="OccupancyPercent">The average occupancy of the months of the period, with one decimal.</param>
/// <param name="ThresholdPercent">The threshold the occupancy crossed.</param>
/// <param name="WasLimited">True when the largest change or the minimum price cut the result.</param>
public sealed record PriceSuggestion(
    RoomSize Size,
    Season Season,
    DateOnly FirstDay,
    DateOnly LastDay,
    decimal Percentage,
    decimal OldPrice,
    decimal NewPrice,
    decimal OccupancyPercent,
    decimal ThresholdPercent,
    bool WasLimited);
