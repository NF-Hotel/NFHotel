namespace NFHotel.Domain.Errors;

/// <summary>
/// Error codes of the stored market analysis (MIL-026). A code is a stable string; the text for the user is the
/// concern of the presentation layer.
/// </summary>
public static class MarketAnalysisErrors
{
    /// <summary>The run id is missing or is the empty id.</summary>
    public const string RunIdRequired = "market_analysis.run_id_required";

    /// <summary>The run ended before it started.</summary>
    public const string EndBeforeStart = "market_analysis.end_before_start";

    /// <summary>The provider or the model name is too long.</summary>
    public const string ModelInvalid = "market_analysis.model_invalid";

    /// <summary>The content of the report is missing.</summary>
    public const string ContentRequired = "market_analysis.content_required";
}
