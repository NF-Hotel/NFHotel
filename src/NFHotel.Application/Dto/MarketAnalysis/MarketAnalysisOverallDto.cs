namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>
/// The overall interpretation of all analyses and the forecast (result schema 1.2, <c>insights.overall</c>). With the source
/// <c>ai</c> a local AI model wrote it; with the source <c>figures</c> the service built it from the largest finding of each analysis.
/// </summary>
/// <param name="Source"><c>ai</c> or <c>figures</c>.</param>
/// <param name="Label">The label the service gives the text, for example <c>AI-generated</c>.</param>
/// <param name="GeneratedAt">When the text was generated, or <c>null</c> (only with the source <c>ai</c>).</param>
/// <param name="Text">The interpretation, written in Markdown.</param>
/// <param name="Evidence">The numbers that the text rests on, or <c>null</c>.</param>
/// <param name="Recommendations">Two to four recommendations; empty when the source is <c>figures</c>.</param>
public sealed record MarketAnalysisOverallDto(
    string Source,
    string Label,
    string? GeneratedAt,
    string Text,
    string? Evidence,
    IReadOnlyList<MarketAnalysisSuggestionDto> Recommendations)
{
    /// <summary>The source of a text that a local AI model wrote.</summary>
    public const string AiSource = "ai";

    /// <summary>True when the text was written by an AI model, and false when the service calculated it from the figures.</summary>
    public bool IsAi => string.Equals(Source, AiSource, StringComparison.Ordinal);
}
