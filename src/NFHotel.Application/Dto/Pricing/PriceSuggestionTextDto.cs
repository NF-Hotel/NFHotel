namespace NFHotel.Application.Dto.Pricing;

/// <summary>A text on a price suggestion: the explanation of one suggestion or an idea without a price (MIL-071). It never sets a price.</summary>
/// <param name="Text">The text.</param>
/// <param name="AiWritten">True when an AI wrote the text, false when it was built from the figures.</param>
public sealed record PriceSuggestionTextDto(string Text, bool AiWritten);
