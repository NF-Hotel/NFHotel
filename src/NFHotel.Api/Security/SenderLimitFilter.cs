using System.Globalization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

using NFHotel.Api.Contracts;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;

namespace NFHotel.Api.Security;

/// <summary>
/// Counts a request against the limits per caller address of its kind before the action runs, and refuses it with 429,
/// the code <c>booking.too_many_requests</c> and <c>Retry-After</c> when a limit is used up (MIL-087, ADR-0051 decision
/// 2). A booking request that a rule of the use case refuses with 400 or 422 gets its count back.
/// </summary>
/// <param name="kind">What the limits count.</param>
/// <param name="limits">The counts per caller address.</param>
/// <param name="caller">The address of the caller.</param>
/// <param name="events">Writes the event of a refusal.</param>
public sealed class SenderLimitFilter(SenderLimitKind kind, ISenderLimits limits, ICallerAddress caller, BookingFlowEvents events) : IAsyncActionFilter
{
    #region Fields

    private readonly SenderLimitKind _kind = kind;
    private readonly ISenderLimits _limits = limits;
    private readonly ICallerAddress _caller = caller;
    private readonly BookingFlowEvents _events = events;

    #endregion Fields

    #region Public methods

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        string? address = _caller.Address;
        string key = CallerAddressKey.Of(address);
        SenderLimitDecision decision = _kind == SenderLimitKind.Search
            ? _limits.TryCountSearch(key)
            : _limits.TryCountBookingRequest(key);
        if (decision is { IsAllowed: false, Limit: { } limit })
        {
            _events.LimitRefused(limit, address, SecurityEvents.EmailOf(context.ActionArguments.Values));
            context.HttpContext.Response.Headers.RetryAfter = WholeSeconds(decision.RetryAfter);
            context.Result = new ObjectResult(new ApiResponse([ErrorCodes.Booking.TooManyRequests]))
            {
                StatusCode = StatusCodes.Status429TooManyRequests,
            };
            return;
        }

        ActionExecutedContext executed = await next().ConfigureAwait(false);

        // A value that a rule of the use case refuses does not use up a count (decision 2).
        if (_kind == SenderLimitKind.BookingRequest
            && executed is { Exception: null, Result: IStatusCodeActionResult { StatusCode: StatusCodes.Status400BadRequest or StatusCodes.Status422UnprocessableEntity } })
        {
            _limits.GiveBackBookingRequest(key);
        }
    }

    #endregion Public methods

    #region Private methods

    // Retry-After holds whole seconds (RFC 9110 section 10.2.3), rounded up, so a client that waits them finds room.
    private static string WholeSeconds(TimeSpan wait) =>
        Math.Max(1L, (long)Math.Ceiling(wait.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

    #endregion Private methods
}
