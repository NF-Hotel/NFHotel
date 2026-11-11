namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>One price suggestion of the market analysis service (result schema 1.3): a change in percent for a period.</summary>
/// <param name="RoomSize">A room size in lower case, or <c>all</c> for a surcharge.</param>
/// <param name="FirstDate">The first day of the period.</param>
/// <param name="LastDate">The last day of the period.</param>
/// <param name="Change"><c>surcharge</c> or <c>discount</c>.</param>
/// <param name="Percent">The change of the base rate in percent.</param>
/// <param name="Reason"><c>holiday</c>, <c>high_history</c> or <c>low_history</c>.</param>
/// <param name="Holiday">The name of the holiday, or <c>null</c>.</param>
/// <param name="Threshold">The occupancy threshold that was crossed, from 0 to 1.</param>
/// <param name="History">The occupancy in each earlier year.</param>
/// <param name="Explanation">Why, in a sentence or two.</param>
public sealed record MarketAnalysisPriceSuggestionDto(
    string RoomSize,
    DateOnly FirstDate,
    DateOnly LastDate,
    string Change,
    decimal Percent,
    string Reason,
    string? Holiday,
    double Threshold,
    IReadOnlyList<MarketAnalysisPriceHistoryDto> History,
    MarketAnalysisPriceTextDto Explanation);
