using System.Diagnostics.Metrics;

using NFHotel.Application.UseCases;

namespace NFHotel.Web.Security;

/// <summary>
/// Refuses a request to the circuits of the pages (<c>/_blazor</c>) when its caller already has
/// <see cref="CircuitLimit.OpenPerCaller"/> open (ADR-0051 decision 8, SAR-002 BF-02): the answer is 429 and no circuit
/// starts. The refusal writes the security event <c>limit_refused</c> with the limit <c>circuits</c> and the address, in
/// the words, the category and the counter of the events of the API, so the query of the log and the alert
/// <c>BookingFlowRefusals</c> see it as they see the other limits. Every other path passes untouched.
/// </summary>
public static partial class CircuitLimitMiddleware
{
    #region Fields

    /// <summary>The path of the circuits: the negotiation, the web socket and the transports that stand in for it.</summary>
    public const string CircuitPath = "/_blazor";

    /// <summary>The name of the limit in the event and in the counter.</summary>
    public const string LimitName = "circuits";

    /// <summary>The kind of the event, as the API names a refusal by a limit.</summary>
    public const string LimitRefusedKind = "limit_refused";

    /// <summary>The log category of the event: the category of the security events of the API.</summary>
    public const string Category = "NFHotel.Security";

    /// <summary>The meter of the counter: the meter of the security events, which the telemetry of the services sends.</summary>
    public const string MeterName = "NFHotel.Security";

    /// <summary>The counter of the security events, <c>nfhotel_security_events_total</c> in Prometheus.</summary>
    public const string EventsCounterName = "nfhotel.security.events";

    /// <summary>The label of the counter with the kind of the event.</summary>
    public const string KindTag = "kind";

    /// <summary>The label of the counter with the name of the limit.</summary>
    public const string LimitTag = "limit";

    // What the event holds for a request without an address, as the events of the API.
    private const string NoAddress = "-";

    #endregion Fields

    #region Public methods

    /// <summary>
    /// Adds the step to the pipeline, before the sign-in reads its cookie, so a refused request costs no more. It needs a
    /// <see cref="CircuitLimit"/> among the services.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The application.</returns>
    public static IApplicationBuilder UseCircuitLimit(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        CircuitLimit limit = app.ApplicationServices.GetRequiredService<CircuitLimit>();
        ILogger logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger(Category);

        // The factory owns the meter and disposes of it with the host.
        Counter<long> events = app.ApplicationServices.GetRequiredService<IMeterFactory>()
            .Create(MeterName)
            .CreateCounter<long>(
                EventsCounterName,
                unit: "{event}",
                description: "The security events of the booking flow, by kind and by the limit of a refusal.");

        return app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments(CircuitPath, StringComparison.OrdinalIgnoreCase))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            string? address = ClientAddressMiddleware.AddressOf(context);
            string caller = CallerAddressKey.Of(address);
            if (!limit.TryOpen(caller))
            {
                logger.LimitRefused(LimitName, address ?? NoAddress);
                events.Add(1, new KeyValuePair<string, object?>(KindTag, LimitRefusedKind), new KeyValuePair<string, object?>(LimitTag, LimitName));
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                return;
            }

            // The web socket of a circuit returns here only when the circuit ends, or when the browser goes away.
            try
            {
                await next(context).ConfigureAwait(false);
            }
            finally
            {
                limit.Close(caller);
            }
        });
    }

    #endregion Public methods

    #region Private methods

    // The id and the words of the event of the API (SecurityEvents.LimitRefused), so one query of the log finds both.
    [LoggerMessage(EventId = 8604, EventName = LimitRefusedKind, Level = LogLevel.Warning,
        Message = LimitRefusedKind + " {Limit} {Address}")]
    private static partial void LimitRefused(this ILogger logger, string limit, string address);

    #endregion Private methods
}
