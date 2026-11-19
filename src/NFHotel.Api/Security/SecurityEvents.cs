using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;

namespace NFHotel.Api.Security;

/// <summary>
/// The security events of the API (MIL-086, ADR-0050 decision 6; MIL-087, ADR-0051 decision 6): one log category, per
/// refused request one event with the method, the route template, the status and the codes, and the events of the
/// booking flow with the address of the caller and a masked email. An event never holds another value of the request,
/// its path or its query as sent.
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

    /// <summary>What an event holds where the request has no email or no address.</summary>
    public const string None = "-";

    /// <summary>What stands for the hidden part of a masked email.</summary>
    private const string Mask = "***";

    /// <summary>The longest domain that a mask keeps (RFC 1035 section 2.3.4).</summary>
    private const int MaxDomainLength = 253;

    #endregion Fields

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

    #endregion Public methods

    #region Private methods

    private static bool IsPlainDomain(string domain) =>
        domain.Length is > 0 and <= MaxDomainLength
        && domain.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-');

    #endregion Private methods
}
