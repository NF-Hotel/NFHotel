namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>The price suggestions of a run (result schema 1.3, <c>price_suggestions</c>).</summary>
/// <param name="AsOf">The first day a suggestion may start.</param>
/// <param name="HorizonEnd">The last day the suggestions reach.</param>
/// <param name="Formula">How the suggestions are worked out.</param>
/// <param name="Limitations">What the suggestions cannot say.</param>
/// <param name="Suggestions">The suggestions.</param>
/// <param name="NotEnoughHistory">The periods that got no suggestion for want of earlier years.</param>
/// <param name="Ideas">Further ideas without a price, written by an AI; empty when the AI is off.</param>
public sealed record MarketAnalysisPriceSuggestionsDto(
    DateOnly AsOf,
    DateOnly HorizonEnd,
    string Formula,
    IReadOnlyList<string> Limitations,
    IReadOnlyList<MarketAnalysisPriceSuggestionDto> Suggestions,
    IReadOnlyList<MarketAnalysisPriceGapDto> NotEnoughHistory,
    IReadOnlyList<MarketAnalysisPriceTextDto> Ideas);
