namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>
/// The content of a market analysis report (result schema version 1.x of the markets API): the headline figures from the data
/// quality summary and one section per analysis with its charts and its AI insight.
/// </summary>
/// <param name="Figures">The headline figures.</param>
/// <param name="Sections">The analyses in the order of the markets API.</param>
public sealed record MarketAnalysisContentDto(
    IReadOnlyList<MarketAnalysisFigureDto> Figures,
    IReadOnlyList<MarketAnalysisSectionDto> Sections);
