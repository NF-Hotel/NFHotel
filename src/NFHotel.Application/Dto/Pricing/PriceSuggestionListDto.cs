namespace NFHotel.Application.Dto.Pricing;

/// <summary>The open price suggestions of the day (MIL-055).</summary>
/// <param name="Basis"><see cref="PriceSuggestionBasis.Ok"/> or <see cref="PriceSuggestionBasis.NoBasis"/>.</param>
/// <param name="GeneratedForDay">The day the suggestions were made for (today in the hotel time zone).</param>
/// <param name="Items">The open suggestions ordered by first day and room size; empty without a basis.</param>
/// <param name="AiExplanation">
/// The executive summary of the AI insight on the occupancy in the same report, or <c>null</c>. It is text for the Manager
/// to read; it never influences a figure.
/// </param>
/// <param name="Ideas">Further ideas without a price from the service (MIL-071); <c>null</c> or empty when there are none.</param>
public sealed record PriceSuggestionListDto(
    string Basis,
    DateOnly GeneratedForDay,
    IReadOnlyList<PriceSuggestionItemDto> Items,
    string? AiExplanation,
    IReadOnlyList<PriceSuggestionTextDto>? Ideas = null);
