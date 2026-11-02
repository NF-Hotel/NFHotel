namespace NFHotel.Domain.Errors;

/// <summary>
/// Error codes of the price changes of a room size (MIL-051). A code is a stable string; the text for the user is the
/// concern of the presentation layer.
/// </summary>
public static class PriceChangeErrors
{
    /// <summary>The room size is not one of the known ones.</summary>
    public const string SizeInvalid = "price_change.size_invalid";

    /// <summary>The percentage is 0, at or below -100, or above 1000.</summary>
    public const string PercentageInvalid = "price_change.percentage_invalid";

    /// <summary>The last day is before the first day, or the period is longer than 366 days.</summary>
    public const string PeriodInvalid = "price_change.period_invalid";

    /// <summary>The name of the person who approved the change is missing or too long.</summary>
    public const string ApprovedByInvalid = "price_change.approved_by_invalid";

    /// <summary>The name of the person who reverts the change is missing or too long.</summary>
    public const string RevertedByInvalid = "price_change.reverted_by_invalid";

    /// <summary>The change is already reverted.</summary>
    public const string AlreadyReverted = "price_change.already_reverted";

    /// <summary>The last day of the change is past, so there is nothing left to revert.</summary>
    public const string PeriodEnded = "price_change.period_ended";

    /// <summary>No price change has the given key.</summary>
    public const string NotFound = "price_change.not_found";

    /// <summary>Another change of the same room size already counts on a day of the period.</summary>
    public const string Overlap = "price_change.overlap";
}
