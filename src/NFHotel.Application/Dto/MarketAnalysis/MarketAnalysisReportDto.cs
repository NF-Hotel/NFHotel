namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>A stored market analysis for the page: the header of the run and its content.</summary>
/// <param name="RunId">The run id of the data analysis service.</param>
/// <param name="StartedAt">When the run started.</param>
/// <param name="FinishedAt">When the run ended.</param>
/// <param name="Provider">The AI provider, or <c>null</c>.</param>
/// <param name="Model">The AI model, or <c>null</c>.</param>
/// <param name="Content">The summary, figures and charts.</param>
public sealed record MarketAnalysisReportDto(
    Guid RunId,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    string? Provider,
    string? Model,
    MarketAnalysisContentDto Content);
