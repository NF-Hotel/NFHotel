namespace NFHotel.Domain.Errors;

/// <summary>
/// Error codes of the market details of a booking (MIL-026). A code is a stable string; the text for the user is the
/// concern of the presentation layer.
/// </summary>
public static class BookingMarketDetailErrors
{
    /// <summary>The booking id is missing.</summary>
    public const string BookingRequired = "booking_market_detail.booking_required";

    /// <summary>A count of guests, changes, requests or cancellations is negative or too large.</summary>
    public const string CountInvalid = "booking_market_detail.count_invalid";

    /// <summary>The price per night is negative or too large.</summary>
    public const string PriceInvalid = "booking_market_detail.price_invalid";

    /// <summary>A text (meal, market segment, customer type, deposit type, room type) is too long.</summary>
    public const string TextInvalid = "booking_market_detail.text_invalid";
}
