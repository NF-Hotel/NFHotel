using System.Globalization;

using NFHotel.Application.UseCases.Bookings;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// Læser et bookingnummer fra en URL tilbage til bookingens id.
/// </summary>
/// <remarks>
/// Kvitteringssiden slår op på nummeret (BR-C-06), men <c>IBookingService</c> har med vilje
/// ingen "hent efter nummer": nummeret <i>er</i> id'et i vist form (BR-109), og en ekstra
/// use case ville være samme opslag med en anden nøgle.
/// <para>
/// Parsningen hører derfor i Web, som spejling af
/// <see cref="BookingMapping.FormatBookingNumber(int)"/>, og ikke i Application.
/// Præfikset er valgfrit og ufølsomt for store og små bogstaver, så et nummer skrevet af
/// fra en kvittering også virker — det, der afvises, er alt hvad der ikke er et positivt
/// heltal, så en tastefejl bliver til "bookingen findes ikke" frem for til et opslag på
/// et tilfældigt id.
/// </para>
/// </remarks>
public static class BookingNumberText
{
    /// <summary>Præfikset i det viste bookingnummer (BR-109).</summary>
    private const string Prefix = "FLZ-";

    /// <summary>Flest cifre et bookingnummer kan bære, uden at <c>int</c> løber over.</summary>
    private const int MaxDigits = 9;

    /// <summary>
    /// Forsøger at læse et bookingnummer som <c>FLZ-000123</c> eller <c>123</c> til et id.
    /// </summary>
    /// <param name="bookingNumber">Nummeret fra URL'en. Må være <c>null</c>.</param>
    /// <param name="bookingId">Bookingens id ved succes; ellers 0.</param>
    /// <returns>Sand hvis nummeret kunne læses som et positivt id.</returns>
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

        // NumberStyles.None: ingen fortegn, ingen tusindtalsseparator, ingen mellemrum —
        // kun de cifre der lige er kontrolleret.
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            return false;
        }

        bookingId = parsed;

        return true;
    }
}