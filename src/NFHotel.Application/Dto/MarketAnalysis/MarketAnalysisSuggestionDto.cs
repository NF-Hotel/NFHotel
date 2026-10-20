namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>One improvement suggestion of the AI for an analysis; a hypothesis with its evidence, not a forecast.</summary>
/// <param name="Suggestion">The suggestion.</param>
/// <param name="Evidence">The finding that it rests on.</param>
/// <param name="SampleSize">The number of bookings behind the evidence.</param>
public sealed record MarketAnalysisSuggestionDto(string Suggestion, string Evidence, int SampleSize);
