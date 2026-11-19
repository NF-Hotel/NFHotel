using NFHotel.Api.Contracts;

namespace NFHotel.Api.Security;

/// <summary>
/// The security events of the API (MIL-086, ADR-0050 decision 6): one log category, and per refused request one event
/// with the method, the route template, the status and the codes. An event never holds a value of the request, its
/// path or its query as sent.
/// </summary>
public static partial class SecurityEvents
{
    #region Fields

    /// <summary>The log category of every security event of the API.</summary>
    public const string Category = "NFHotel.Security";

    /// <summary>The route that an event names when the request matched no endpoint.</summary>
    public const string NoRoute = "(no route)";

    #endregion Fields

    #region Public methods

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

    #endregion Public methods
}
