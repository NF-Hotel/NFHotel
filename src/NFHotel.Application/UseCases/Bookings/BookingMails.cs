using System.Globalization;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Bookings;

/// <summary>
/// The two mails of the booking request (MIL-088, ADR-0052 decisions 4 and 5): plain text in English with a fixed subject,
/// built only from values that the API checked, never from a text that the guest typed. The email is only the address
/// that the mail goes to.
/// </summary>
internal static class BookingMails
{
    #region Fields

    /// <summary>The subject of the mail with the link.</summary>
    public const string RequestSubject = "Verify your booking at NF Hotel";

    /// <summary>The subject of the mail with the reference.</summary>
    public const string ReceiptSubject = "Your booking at NF Hotel";

    /// <summary>The path of the page of the link, after the site address.</summary>
    public const string VerifyPath = "booking/verify";

    // A day as the mails write it, for example "Thursday 1 January 2026".
    private const string DateFormat = "dddd d MMMM yyyy";

    private const string Footer = "This mail was sent automatically. Replies to it are not read.";

    #endregion Fields

    #region Public methods

    /// <summary>The address of the page of the link with the token after <c>#</c>, below the path of the site address.</summary>
    /// <param name="siteAddress">The site address of the settings.</param>
    /// <param name="token">The token.</param>
    /// <returns>The link.</returns>
    public static Uri LinkOf(Uri siteAddress, string token)
    {
        ArgumentNullException.ThrowIfNull(siteAddress);
        ArgumentException.ThrowIfNullOrEmpty(token);

        string site = siteAddress.GetLeftPart(UriPartial.Path).TrimEnd('/');
        return new Uri($"{site}/{VerifyPath}#{token}");
    }

    /// <summary>The mail with the link of a request.</summary>
    /// <param name="request">The request.</param>
    /// <param name="link">The link.</param>
    /// <returns>The mail.</returns>
    public static OutgoingMail Request(BookingRequest request, Uri link)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(link);

        DateTime lapses = request.ExpiresAt.UtcDateTime;
        string[] lines =
        [
            "A booking at NF Hotel was asked for with this email address.",
            string.Empty,
            .. Stay(request.StartDate, request.EndDate),
            Invariant($"Room: {RoomName(request.RoomSize)}"),
            Invariant($"Guests: {request.Guests}"),
            string.Empty,
            "To book the room, open this link and press the button on the page:",
            link.AbsoluteUri,
            string.Empty,
            Invariant($"The link works once, until {lapses:HH:mm} UTC on {Day(DateOnly.FromDateTime(lapses))}.")
                + " The room is not booked until you press the button.",
            string.Empty,
            "If you did not ask for this booking, do nothing: the link stops working by itself.",
            string.Empty,
            Footer,
        ];

        return new OutgoingMail(request.Email, RequestSubject, string.Join('\n', lines));
    }

    /// <summary>The mail with the reference of the booking that a verification made.</summary>
    /// <param name="email">The email of the booking.</param>
    /// <param name="receipt">The receipt of the booking.</param>
    /// <param name="siteAddress">The site address of the settings, where the guest finds the booking.</param>
    /// <returns>The mail.</returns>
    public static OutgoingMail Receipt(string email, BookingReceiptDto receipt, Uri siteAddress)
    {
        ArgumentException.ThrowIfNullOrEmpty(email);
        ArgumentNullException.ThrowIfNull(receipt);
        ArgumentNullException.ThrowIfNull(siteAddress);

        string[] lines =
        [
            "Your booking at NF Hotel is made.",
            string.Empty,
            Invariant($"Reference: {receipt.Reference}"),
            .. Stay(receipt.StartDate, receipt.EndDate),
            Invariant($"Room: {RoomName(receipt.RoomSize)}"),
            "Status: Pending until the hotel confirms it.",
            string.Empty,
            "To see or cancel the booking, open the site and use \"Find your booking\" with the reference and this email address:",
            siteAddress.GetLeftPart(UriPartial.Path),
            string.Empty,
            Footer,
        ];

        return new OutgoingMail(email, ReceiptSubject, string.Join('\n', lines));
    }

    #endregion Public methods

    #region Private methods

    private static string[] Stay(DateOnly arrival, DateOnly departure)
    {
        int nights = departure.DayNumber - arrival.DayNumber;
        return
        [
            Invariant($"Arrival: {Day(arrival)}"),
            Invariant($"Departure: {Day(departure)} ({nights} {(nights == 1 ? "night" : "nights")})"),
        ];
    }

    private static string Day(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    private static string RoomName(RoomSize size) => size switch
    {
        RoomSize.Single => "Single room",
        RoomSize.Double => "Double room",
        RoomSize.Suite => "Suite",
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "A room size without a name in the mails."),
    };

    private static string Invariant(FormattableString text) => FormattableString.Invariant(text);

    #endregion Private methods
}
