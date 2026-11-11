namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>
/// The content of a market analysis report (result schema version 1.x of the markets API): the headline figures from the data
/// quality summary and one section per analysis with its charts and its AI insight.
/// </summary>
/// <param name="Figures">The headline figures.</param>
/// <param name="Sections">The analyses in the order of the markets API.</param>
/// <param name="Overall">The overall interpretation (result schema 1.2); <c>null</c> when the result has none, also in a report stored before it existed.</param>
/// <param name="Forecast">The forecast (result schema 1.2); <c>null</c> when the result has none.</param>
/// <param name="PriceSuggestions">The price suggestions (result schema 1.3); <c>null</c> when the result has none, also in a report stored before it existed.</param>
public sealed record MarketAnalysisContentDto(
    IReadOnlyList<MarketAnalysisFigureDto> Figures,
    IReadOnlyList<MarketAnalysisSectionDto> Sections,
    MarketAnalysisOverallDto? Overall = null,
    MarketAnalysisForecastDto? Forecast = null,
    MarketAnalysisPriceSuggestionsDto? PriceSuggestions = null);
