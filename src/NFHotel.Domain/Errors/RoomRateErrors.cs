namespace NFHotel.Domain.Errors;

/// <summary>
/// Error codes of the nightly base rate per room size (MIL-051). A code is a stable string; the text for the user is
/// the concern of the presentation layer.
/// </summary>
public static class RoomRateErrors
{
    /// <summary>The room size is not one of the known ones.</summary>
    public const string SizeInvalid = "room_rate.size_invalid";

    /// <summary>The nightly price is not above 0 or is above the largest price.</summary>
    public const string PriceInvalid = "room_rate.price_invalid";

    /// <summary>The name of the person who set the rate is missing or too long.</summary>
    public const string SetByInvalid = "room_rate.set_by_invalid";

    /// <summary>Another save of the rate of the same size came first; the Manager reloads and tries again.</summary>
    public const string ConcurrencyConflict = "room_rate.concurrency_conflict";
}
