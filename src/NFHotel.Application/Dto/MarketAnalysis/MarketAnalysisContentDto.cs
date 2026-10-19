namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>
/// The content of a market analysis report as the <c>result</c> of the <c>market_analysis</c> notebook delivers it
/// (result schema version 1): the AI analysis, the headline figures and the chart series.
/// </summary>
/// <param name="Summary">The AI analysis as text; empty when no AI model was available.</param>
/// <param name="Figures">The headline figures.</param>
/// <param name="Series">The charts.</param>
public sealed record MarketAnalysisContentDto(
    string Summary,
    IReadOnlyList<MarketAnalysisFigureDto> Figures,
    IReadOnlyList<MarketAnalysisSeriesDto> Series);
