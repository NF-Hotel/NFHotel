namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>One improvement suggestion of the AI for an analysis; a hypothesis with its evidence, not a forecast.</summary>
/// <param name="Suggestion">The suggestion, written in Markdown.</param>
/// <param name="Evidence">The finding that it rests on.</param>
/// <param name="SampleSize">The number of bookings behind the evidence.</param>
/// <param name="Series">The path of the findings that the suggestion reads (result schema 1.2); <c>null</c> when the result has none, also in a report stored before it existed.</param>
public sealed record MarketAnalysisSuggestionDto(string Suggestion, string Evidence, int SampleSize, string? Series = null);
