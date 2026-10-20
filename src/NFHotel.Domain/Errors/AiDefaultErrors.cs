namespace NFHotel.Domain.Errors;

/// <summary>
/// Error codes of the default AI model settings (ADR-0014). A code is a stable string; the text for the user is the
/// concern of the presentation layer.
/// </summary>
public static class AiDefaultErrors
{   

    /// <summary>The purpose is not one of the known ones.</summary>
    public const string PurposeInvalid = "ai_default.purpose_invalid";

    /// <summary>The model name is missing or too long.</summary>
    public const string ModelInvalid = "ai_default.model_invalid";

    /// <summary>The name of the person who set the default is missing or too long.</summary>
    public const string SetByInvalid = "ai_default.set_by_invalid";
}
