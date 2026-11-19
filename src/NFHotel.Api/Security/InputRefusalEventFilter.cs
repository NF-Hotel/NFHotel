using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NFHotel.Api.Security;

/// <summary>
/// Writes the security event of each refusal of input that a controller answers, whether a rule of a use case, the
/// binding or <see cref="InputGuardFilter"/> refused (MIL-086, ADR-0050 decision 6). It runs for every result, a
/// short-circuited one included.
/// </summary>
/// <param name="loggerFactory">The factory of the logger of <see cref="SecurityEvents.Category"/>.</param>
public sealed class InputRefusalEventFilter(ILoggerFactory loggerFactory) : IAlwaysRunResultFilter
{
    #region Fields

    private readonly ILogger _logger = loggerFactory.CreateLogger(SecurityEvents.Category);

    #endregion Fields

    #region Public methods

    /// <inheritdoc/>
    public void OnResultExecuting(ResultExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is { Result: ObjectResult { StatusCode: int status } result }
            && SecurityEvents.IsInputRefusal(status)
            && SecurityEvents.CodesOf(result.Value) is { } codes)
        {
            _logger.InputRefused(
                context.HttpContext.Request.Method,
                context.ActionDescriptor.AttributeRouteInfo?.Template ?? SecurityEvents.NoRoute,
                status,
                codes);
        }
    }

    /// <inheritdoc/>
    public void OnResultExecuted(ResultExecutedContext context)
    {
        // The event is written before the answer.
    }

    #endregion Public methods
}
