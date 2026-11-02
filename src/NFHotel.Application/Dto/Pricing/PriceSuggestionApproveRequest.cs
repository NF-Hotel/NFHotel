namespace NFHotel.Application.Dto.Pricing;

/// <summary>The approval of an open price suggestion (MIL-055). The percentage is never sent: the server takes it from the suggestion.</summary>
/// <param name="Key">The key of the suggestion from <see cref="PriceSuggestionItemDto.Key"/>.</param>
/// <param name="FirstDay">The first day of the change, or <c>null</c> for the first day of the suggestion.</param>
/// <param name="LastDay">The last day of the change, or <c>null</c> for the last day of the suggestion.</param>
public sealed record PriceSuggestionApproveRequest(string Key, DateOnly? FirstDay = null, DateOnly? LastDay = null);
