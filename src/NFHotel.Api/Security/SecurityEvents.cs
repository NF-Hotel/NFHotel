using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Ports;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Security;

/// <summary>
/// The security events of the API (MIL-086, ADR-0050 decision 6; MIL-087, ADR-0051 decision 6): one log category, per
/// refused request one event with the method, the route template, the status and the codes, and the events of the
/// booking flow with the address of the caller and a masked email. An event never holds another value of the request,
/// its path or its query as sent. Since MIL-088 also the booking request, its verification and its mails (ADR-0052
/// decision 10, ADR-0053 decisions 5 and 6); an event of a mail holds its kind and a reason in a word, never an address.
/// </summary>
public static partial class SecurityEvents
{
    #region Fields

    /// <summary>The log category of every security event of the API.</summary>
    public const string Category = "NFHotel.Security";

    /// <summary>The route that an event names when the request matched no endpoint.</summary>
    public const string NoRoute = "(no route)";

    /// <summary>The kind of the event of a booking made on the public site (MIL-087, ADR-0051 decision 6).</summary>
    public const string PublicBookingMadeKind = "public_booking_made";

    /// <summary>The kind of the event of a guest sign-in refused for a wrong reference or email.</summary>
    public const string GuestLookupFailedKind = "guest_lookup_failed";

    /// <summary>The kind of the event of a refusal by a limit of the booking flow.</summary>
    public const string LimitRefusedKind = "limit_refused";

    /// <summary>The kind of the event of a booking request that was stored and whose mail went (MIL-088).</summary>
    public const string BookingRequestMadeKind = "booking_request_made";

    /// <summary>The kind of the event of a verification with a link that does not work (MIL-088).</summary>
    public const string VerificationFailedKind = "verification_failed";

    /// <summary>The kind of the event of a mail that the relay did not take or that the fuse held back (MIL-088).</summary>
    public const string MailNotSentKind = "mail_not_sent";

    /// <summary>The kind of the event of a request or a mail refused by the fuse of the mails a day (MIL-088).</summary>
    public const string MailFuseBlownKind = "mail_fuse_blown";

    /// <summary>The name of the kind of the mail of a booking request, with the link that verifies it.</summary>
    public const string VerificationLinkMail = "verification_link";

    /// <summary>The name of the kind of the mail after a verification, with the reference.</summary>
    public const string BookingReceiptMail = "booking_receipt";

    /// <summary>What an event holds where the request has no email or no address.</summary>
    public const string None = "-";

    /// <summary>What stands for the hidden part of a masked email.</summary>
    private const string Mask = "***";

    /// <summary>The longest domain that a mask keeps (RFC 1035 section 2.3.4).</summary>
    private const int MaxDomainLength = 253;

    #endregion Fields

    #region Properties

    /// <summary>The names of every kind of mail, as the events and the counter of the mails hold them.</summary>
    public static IReadOnlyList<string> MailNames { get; } = [VerificationLinkMail, BookingReceiptMail];

    #endregion Properties

    #region Public methods

    /// <summary>
    /// Masks an email to its first character and its domain, such as <c>a***@example.com</c> (ADR-0051 decision 6), so an
    /// event tells two senders apart without holding the address that a guest typed.
    /// </summary>
    /// <param name="email">The email as the request has it, or <see langword="null"/>.</param>
    /// <returns>The masked email, or <see cref="None"/>.</returns>
    /// <remarks>
    /// The first character is kept only when it is an ASCII letter or digit, and the domain only when it is made of ASCII
    /// letters, digits, dots and hyphens, so a typed value can never write a line break, a separator or another field
    /// into the log.
    /// </remarks>
    public static string MaskEmail(string? email)
    {
        string text = email?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            return None;
        }

        string first = char.IsAsciiLetterOrDigit(text[0]) ? text[..1] : string.Empty;
        int at = text.LastIndexOf('@');
        string domain = at > 0 ? text[(at + 1)..] : string.Empty;
        return IsPlainDomain(domain) ? $"{first}{Mask}@{domain}" : $"{first}{Mask}";
    }

    /// <summary>The name of a kind of mail in an event and in the counter of the mails (MIL-088).</summary>
    /// <param name="kind">The kind of the mail.</param>
    /// <returns>The name, such as <c>verification_link</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is no kind of mail.</exception>
    public static string MailNameOf(MailKind kind) => kind switch
    {
        MailKind.VerificationLink => VerificationLinkMail,
        MailKind.BookingReceipt => BookingReceiptMail,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "No kind of mail."),
    };

    /// <summary>
    /// The reason of a mail not sent as an event holds it: one of <see cref="MailFailureReasons"/>, and
    /// <see cref="MailFailureReasons.Other"/> for any other text, so no text of a relay reaches the log.
    /// </summary>
    /// <param name="reason">The reason that the use case gives.</param>
    /// <returns>The reason in a word.</returns>
    public static string MailReasonOf(string? reason) =>
        reason is not null && MailFailureReasons.All.Contains(reason, StringComparer.Ordinal) ? reason : MailFailureReasons.Other;

    /// <summary>The email of a request of the booking flow: the email of the new guest of a public booking or of a sign-in.</summary>
    /// <param name="arguments">The arguments of the action.</param>
    /// <returns>The email, or <see langword="null"/>.</returns>
    public static string? EmailOf(IEnumerable<object?> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        return arguments
            .Select(argument => argument switch
            {
                CreatePublicBookingRequest booking => booking.NewGuest?.Email,
                GuestSignInRequest signIn => signIn.Email,
                _ => null,
            })
            .FirstOrDefault(email => email is not null);
    }

    /// <summary>Tells whether a status is a refusal of input: 400, 413 or 422.</summary>
    /// <param name="status">The status of the answer.</param>
    /// <returns><see langword="true"/> for the statuses that write the event.</returns>
    public static bool IsInputRefusal(int status) =>
        status is StatusCodes.Status400BadRequest or StatusCodes.Status413PayloadTooLarge or StatusCodes.Status422UnprocessableEntity;

    /// <summary>The route template of an endpoint, such as <c>api/bookings/public</c>.</summary>
    /// <param name="endpoint">The endpoint, or <see langword="null"/>.</param>
    /// <returns>The template, or <see cref="NoRoute"/>.</returns>
    public static string RouteOf(Endpoint? endpoint) => (endpoint as RouteEndpoint)?.RoutePattern.RawText ?? NoRoute;

    /// <summary>The codes of an answer of the API, joined by commas.</summary>
    /// <param name="value">The body of the answer.</param>
    /// <returns>The codes, or <see langword="null"/> when the body is no answer of the API.</returns>
    public static string? CodesOf(object? value)
    {
        IReadOnlyList<string>? errors = value switch
        {
            ApiResponse response => response.Errors,
            not null when value.GetType() is { IsGenericType: true } type && type.GetGenericTypeDefinition() == typeof(ApiResponse<>) =>
                type.GetProperty(nameof(ApiResponse.Errors))?.GetValue(value) as IReadOnlyList<string>,
            _ => null,
        };

        return errors is null ? null : string.Join(",", errors);
    }

    /// <summary>Writes the event of a request refused for its input with a logger from the services of the request.</summary>
    /// <param name="context">The request.</param>
    /// <param name="route">The route template of its endpoint.</param>
    /// <param name="status">The status of the answer.</param>
    /// <param name="codes">The codes of the answer.</param>
    public static void WriteInputRefused(HttpContext context, string route, int status, string codes)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(Category)
            .InputRefused(context.Request.Method, route, status, codes);
    }

    /// <summary>Writes the event of a request refused for its input, at the level Warning.</summary>
    /// <param name="logger">A logger of <see cref="Category"/>.</param>
    /// <param name="method">The method of the request.</param>
    /// <param name="route">The route template of the endpoint.</param>
    /// <param name="status">The status of the answer.</param>
    /// <param name="codes">The codes of the answer.</param>
    [LoggerMessage(EventId = 8601, EventName = "input_refused", Level = LogLevel.Warning,
        Message = "input_refused {Method} {Route} {Status} {Codes}")]
    public static partial void InputRefused(this ILogger logger, string method, string route, int status, string codes);

    /// <summary>Writes the event of a booking made on the public site, at the level Information.</summary>
    /// <param name="logger">A logger of <see cref="Category"/>.</param>
    /// <param name="address">The address of the caller.</param>
    /// <param name="email">The masked email of the booking.</param>
    [LoggerMessage(EventId = 8602, EventName = PublicBookingMadeKind, Level = LogLevel.Information,
        Message = PublicBookingMadeKind + " {Address} {Email}")]
    public static partial void PublicBookingMade(this ILogger logger, string address, string email);

    /// <summary>Writes the event of a guest sign-in refused for a wrong reference or email, at the level Warning.</summary>
    /// <param name="logger">A logger of <see cref="Category"/>.</param>
    /// <param name="address">The address of the caller.</param>
    /// <param name="email">The masked email that was tried.</param>
    [LoggerMessage(EventId = 8603, EventName = GuestLookupFailedKind, Level = LogLevel.Warning,
        Message = GuestLookupFailedKind + " {Address} {Email}")]
    public static partial void GuestLookupFailed(this ILogger logger, string address, string email);

    /// <summary>Writes the event of a refusal by a limit of the booking flow, at the level Warning.</summary>
    /// <param name="logger">A logger of <see cref="Category"/>.</param>
    /// <param name="limit">The name of the limit.</param>
    /// <param name="address">The address of the caller.</param>
    /// <param name="email">The masked email of the request, or <see cref="None"/>.</param>
    [LoggerMessage(EventId = 8604, EventName = LimitRefusedKind, Level = LogLevel.Warning,
        Message = LimitRefusedKind + " {Limit} {Address} {Email}")]
    public static partial void LimitRefused(this ILogger logger, string limit, string address, string email);

    /// <summary>Writes the event of a booking request that was stored and whose mail went, at the level Information.</summary>
    /// <param name="logger">A logger of <see cref="Category"/>.</param>
    /// <param name="address">The address of the caller.</param>
    /// <param name="email">The masked email of the request.</param>
    [LoggerMessage(EventId = 8605, EventName = BookingRequestMadeKind, Level = LogLevel.Information,
        Message = BookingRequestMadeKind + " {Address} {Email}")]
    public static partial void BookingRequestMade(this ILogger logger, string address, string email);

    /// <summary>Writes the event of a verification with a link that does not work, at the level Warning.</summary>
    /// <param name="logger">A logger of <see cref="Category"/>.</param>
    /// <param name="address">The address of the caller.</param>
    [LoggerMessage(EventId = 8606, EventName = VerificationFailedKind, Level = LogLevel.Warning,
        Message = VerificationFailedKind + " {Address}")]
    public static partial void VerificationFailed(this ILogger logger, string address);

    /// <summary>Writes the event of a mail that was not sent, at the level Error.</summary>
    /// <param name="logger">A logger of <see cref="Category"/>.</param>
    /// <param name="mail">The name of the kind of the mail.</param>
    /// <param name="reason">The reason in a word.</param>
    [LoggerMessage(EventId = 8607, EventName = MailNotSentKind, Level = LogLevel.Error,
        Message = MailNotSentKind + " {Mail} {Reason}")]
    public static partial void MailNotSent(this ILogger logger, string mail, string reason);

    /// <summary>Writes the event of a refusal by the fuse of the mails a day, at the level Error.</summary>
    /// <param name="logger">A logger of <see cref="Category"/>.</param>
    [LoggerMessage(EventId = 8608, EventName = MailFuseBlownKind, Level = LogLevel.Error,
        Message = MailFuseBlownKind)]
    public static partial void MailFuseBlown(this ILogger logger);

    #endregion Public methods

    #region Private methods

    private static bool IsPlainDomain(string domain) =>
        domain.Length is > 0 and <= MaxDomainLength
        && domain.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-');

    #endregion Private methods
}
