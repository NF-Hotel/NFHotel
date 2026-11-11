namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>A text of a price suggestion, marked as written by an AI or built from the figures (result schema 1.3).</summary>
/// <param name="Text">The text, in Markdown.</param>
/// <param name="AiWritten">True when an AI model wrote it; false when the service built it from the figures.</param>
public sealed record MarketAnalysisPriceTextDto(string Text, bool AiWritten);
