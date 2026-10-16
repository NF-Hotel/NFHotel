using System.Globalization;

using NFHotel.Application.UseCases.Bookings;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// Reads a booking number from a URL back to the id of the booking.
/// </summary>
/// <remarks>
/// The receipt page looks up the number (BR-C-06), but <c>IBookingService</c> deliberately has
/// no "fetch by number": the number <i>is</i> the id in displayed form (BR-109), and an extra
/// use case would be the same lookup with a different key.
/// <para>
/// The parsing therefore belongs in Web, as a mirror of
/// <see cref="BookingMapping.FormatBookingNumber(int)"/>, and not in Application.
/// The prefix is optional and case-insensitive, so a number copied from
/// a receipt also works — what is rejected is everything that is not a positive
/// integer, so a typing mistake becomes "the booking does not exist" rather than a lookup on
/// a random id.
/// </para>
/// </remarks>
public static class BookingNumberText
{
    /// <summary>The prefix in the displayed booking number (BR-109).</summary>
    private const string Prefix = "FLZ-";

    /// <summary>The most digits a booking number can carry without <c>int</c> overflowing.</summary>
    private const int MaxDigits = 9;

    /// <summary>
    /// Tries to read a booking number such as <c>FLZ-000123</c> or <c>123</c> as an id.
    /// </summary>
    /// <param name="bookingNumber">The number from the URL. May be <c>null</c>.</param>
    /// <param name="bookingId">The id of the booking on success; otherwise 0.</param>
    /// <returns>True if the number could be read as a positive id.</returns>
    public static bool TryParseId(string? bookingNumber, out int bookingId)
    {
        bookingId = 0;

        if (string.IsNullOrWhiteSpace(bookingNumber))
        {
            return false;
        }

        var text = bookingNumber.Trim();

        if (text.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            text = text[Prefix.Length..];
        }

        if (text.Length is 0 or > MaxDigits)
        {
            return false;
        }

        foreach (var character in text)
        {
            if (!char.IsAsciiDigit(character))
            {
                return false;
            }
        }

        // NumberStyles.None: no sign, no thousands separator, no spaces —
        // only the digits that have just been checked.
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            return false;
        }

        bookingId = parsed;

        return true;
    }
}