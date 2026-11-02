namespace NFHotel.Domain.Errors;

/// <summary>
/// Error codes of the price suggestions (MIL-055). A code is a stable string; the text for the user is the concern of the
/// presentation layer.
/// </summary>
public static class PriceSuggestionErrors
{
    /// <summary>The key does not match any suggestion that is open now, for example because the report or the settings changed.</summary>
    public const string NotFound = "price_suggestion.not_found";
}
