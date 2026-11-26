using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

using NFHotel.Application.Ports;

namespace NFHotel.Api.Security;

/// <summary>
/// Writes the security event of the outcome of a step of the booking flow (MIL-087, ADR-0051 decision 6): a booking made
/// on the public site, a guest sign-in refused for a wrong reference or email, and the refusals of the limit per mailbox
/// and of the limits of the guest sign-in. It reads no value of the request: each event holds the address of the caller.
/// </summary>
/// <param name="step">The step of the action.</param>
/// <param name="caller">The address of the caller.</param>
/// <param name="events">Writes the events.</param>
public sealed class BookingFlowEventFilter(BookingFlowStep step, ICallerAddress caller, BookingFlowEvents events) : IAsyncActionFilter
{
    #region Fields

    private readonly BookingFlowStep _step = step;
    private readonly ICallerAddress _caller = caller ?? throw new ArgumentNullException(nameof(caller));
    private readonly BookingFlowEvents _events = events ?? throw new ArgumentNullException(nameof(events));

    #endregion Fields

    #region Public methods

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        ActionExecutedContext executed = await next().ConfigureAwait(false);
        if (executed is not { Exception: null, Result: IStatusCodeActionResult { StatusCode: { } status } })
        {
            return;
        }

        switch (_step, status)
        {
            case (BookingFlowStep.PublicBooking, StatusCodes.Status200OK):
                _events.PublicBookingMade(_caller.Address);
                break;
            case (BookingFlowStep.PublicBooking, StatusCodes.Status429TooManyRequests):
                _events.LimitRefused(LimitNames.Mailbox, _caller.Address);
                break;
            case (BookingFlowStep.GuestSignIn, StatusCodes.Status401Unauthorized):
                _events.GuestLookupFailed(_caller.Address);
                break;
            case (BookingFlowStep.GuestSignIn, StatusCodes.Status429TooManyRequests):
                _events.LimitRefused(LimitNames.GuestSignIn, _caller.Address);
                break;
            default:
                break;
        }
    }

    #endregion Public methods
}
