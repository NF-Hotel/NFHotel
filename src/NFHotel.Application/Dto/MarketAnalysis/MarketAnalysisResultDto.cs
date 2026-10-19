namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>
/// The result of a run as <c>GET /api/v1/runs/{job_id}/result</c> of the data analysis service delivers it: the
/// metadata, the schema version and the stored result. The sample is <c>docs/test-data/market-analysis-result.json</c>.
/// </summary>
/// <param name="RunId">The job id.</param>
/// <param name="SchemaVersion">The version of the result schema.</param>
/// <param name="StartedAt">When the run started.</param>
/// <param name="FinishedAt">When the run ended.</param>
/// <param name="Provider">The AI provider, or <c>null</c>.</param>
/// <param name="Model">The AI model, or <c>null</c>.</param>
/// <param name="Content">The summary, figures and charts; <c>null</c> when the service sent none.</param>
public sealed record MarketAnalysisResultDto(
    Guid RunId,
    int SchemaVersion,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    string? Provider,
    string? Model,
    MarketAnalysisContentDto? Content);
